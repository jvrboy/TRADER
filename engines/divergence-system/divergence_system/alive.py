"""
alive.py — The ALIVE divergence engine.

This is the heartbeat of the system: a continuously running scanner that
treats divergences as living objects with a lifecycle rather than one-shot
events.

Lifecycle of a divergence:

    FORMING    → pivots printed but right-side confirmation bars pending
    CONFIRMED  → fully confirmed, actionable, being tracked
    PLAYING_OUT→ price started moving in the divergence direction
    COMPLETED  → price reached the measured objective (ATR multiple)
    INVALIDATED→ price broke beyond the divergence pivot (signal failed)
    EXPIRED    → too old without resolution

The engine deduplicates signals across scan cycles (a divergence seen on the
previous cycle is UPDATED, not re-alerted), tracks state transitions, and
emits events through pluggable alert handlers (console / JSON-lines file /
webhook / custom callback).
"""

from __future__ import annotations

import hashlib
import json
import time
from dataclasses import dataclass, field
from datetime import datetime, timezone

import pandas as pd

from .divergence import Divergence
from .indicators import atr
from .mtf import ConfluenceReport, MultiTimeframeScanner, resample_ohlcv


# ----------------------------------------------------------------------------
# Alert handlers
# ----------------------------------------------------------------------------

class ConsoleAlertHandler:
    COLORS = {"bullish": "\033[92m", "bearish": "\033[91m"}
    RESET = "\033[0m"

    def __call__(self, event: dict) -> None:
        color = self.COLORS.get(event.get("direction", ""), "")
        print(
            f"{color}[{event['timestamp']}] {event['event']:<12} "
            f"{event['symbol']:<12} {event.get('timeframe',''):<4} "
            f"{event.get('indicator',''):<10} {event.get('signal',''):<16} "
            f"score={event.get('score','-')} grade={event.get('grade','-')}{self.RESET}"
        )


class JsonlAlertHandler:
    def __init__(self, path: str = "divergence_events.jsonl") -> None:
        self.path = path

    def __call__(self, event: dict) -> None:
        with open(self.path, "a", encoding="utf-8") as f:
            f.write(json.dumps(event, default=str) + "\n")


class WebhookAlertHandler:
    """POSTs each event as JSON. Works with Discord/Slack-style webhooks
    if you wrap the payload appropriately in `format_payload`."""

    def __init__(self, url: str) -> None:
        self.url = url

    def format_payload(self, event: dict) -> dict:
        return {"content": json.dumps(event, default=str)}

    def __call__(self, event: dict) -> None:
        try:
            import urllib.request

            req = urllib.request.Request(
                self.url,
                data=json.dumps(self.format_payload(event)).encode(),
                headers={"Content-Type": "application/json"},
            )
            urllib.request.urlopen(req, timeout=10)
        except Exception as exc:  # never let alerting kill the engine
            print(f"[alive] webhook error: {exc}")


# ----------------------------------------------------------------------------
# Tracked (living) divergence
# ----------------------------------------------------------------------------

@dataclass
class TrackedDivergence:
    key: str
    divergence: Divergence
    state: str = "CONFIRMED"
    born_at: datetime = field(default_factory=lambda: datetime.now(timezone.utc))
    last_seen_cycle: int = 0
    target_price: float = 0.0
    invalidation_price: float = 0.0
    max_favorable_move: float = 0.0

    def to_event(self, event: str) -> dict:
        d = self.divergence
        return {
            "event": event,
            "timestamp": datetime.now(timezone.utc).isoformat(timespec="seconds"),
            "key": self.key,
            "symbol": d.symbol,
            "timeframe": d.timeframe,
            "indicator": d.indicator,
            "signal": d.signal,
            "direction": d.direction,
            "score": d.score,
            "grade": d.grade,
            "state": self.state,
            "target_price": round(self.target_price, 8),
            "invalidation_price": round(self.invalidation_price, 8),
        }


def _divergence_key(d: Divergence) -> str:
    raw = f"{d.symbol}|{d.timeframe}|{d.indicator}|{d.signal}|{d.start_time}|{d.end_time}"
    return hashlib.sha1(raw.encode()).hexdigest()[:16]


# ----------------------------------------------------------------------------
# The engine
# ----------------------------------------------------------------------------

class AliveDivergenceEngine:
    """Continuously scans symbols, tracks divergence lifecycles, emits events."""

    def __init__(
        self,
        scanner: MultiTimeframeScanner,
        fetch_fn,                      # fetch_fn(symbol) -> base OHLCV DataFrame
        symbols: list[str],
        alert_handlers: list | None = None,
        target_atr_multiple: float = 2.0,
        expiry_bars: int = 40,
        poll_seconds: float = 60.0,
    ) -> None:
        self.scanner = scanner
        self.fetch_fn = fetch_fn
        self.symbols = symbols
        self.alert_handlers = alert_handlers or [ConsoleAlertHandler()]
        self.target_atr_multiple = target_atr_multiple
        self.expiry_bars = expiry_bars
        self.poll_seconds = poll_seconds

        self.tracked: dict[str, TrackedDivergence] = {}
        self.cycle: int = 0
        self.last_confluence: dict[str, list[ConfluenceReport]] = {}

    # ------------------------------------------------------------------
    def _emit(self, event: dict) -> None:
        for handler in self.alert_handlers:
            try:
                handler(event)
            except Exception as exc:
                print(f"[alive] alert handler error: {exc}")

    # ------------------------------------------------------------------
    def _levels_for(self, d: Divergence, base_df: pd.DataFrame) -> tuple[float, float]:
        """Compute measured-move target and invalidation for a divergence
        using ATR on its own timeframe."""
        df_tf = resample_ohlcv(base_df, d.timeframe)
        a = atr(df_tf["high"], df_tf["low"], df_tf["close"], 14)
        last_atr = float(a.iloc[-1]) if len(a) else 0.0
        entry = float(df_tf["close"].iloc[-1])
        if d.direction == "bullish":
            target = entry + self.target_atr_multiple * last_atr
            invalidation = min(d.price_end, d.price_start) - 0.25 * last_atr
        else:
            target = entry - self.target_atr_multiple * last_atr
            invalidation = max(d.price_end, d.price_start) + 0.25 * last_atr
        return target, invalidation

    # ------------------------------------------------------------------
    def _update_lifecycle(self, t: TrackedDivergence, last_price: float) -> None:
        d = t.divergence
        if t.state in ("COMPLETED", "INVALIDATED", "EXPIRED"):
            return

        bullish = d.direction == "bullish"

        # Invalidation: price broke the pivot the divergence stands on
        if (bullish and last_price <= t.invalidation_price) or (
            not bullish and last_price >= t.invalidation_price
        ):
            t.state = "INVALIDATED"
            self._emit(t.to_event("INVALIDATED"))
            return

        # Completion: measured target hit
        if (bullish and last_price >= t.target_price) or (
            not bullish and last_price <= t.target_price
        ):
            t.state = "COMPLETED"
            self._emit(t.to_event("COMPLETED"))
            return

        # Playing out: moved favorably by at least 30% of the target distance
        entry_to_target = abs(t.target_price - t.invalidation_price) or 1e-12
        favorable = (last_price - t.invalidation_price) if bullish else (t.invalidation_price - last_price)
        progress = favorable / entry_to_target
        t.max_favorable_move = max(t.max_favorable_move, progress)
        if t.state == "CONFIRMED" and progress >= 0.55:
            t.state = "PLAYING_OUT"
            self._emit(t.to_event("PLAYING_OUT"))

        # Expiry
        if self.cycle - t.last_seen_cycle > self.expiry_bars:
            t.state = "EXPIRED"
            self._emit(t.to_event("EXPIRED"))

    # ------------------------------------------------------------------
    def scan_once(self) -> dict[str, list[ConfluenceReport]]:
        """Run one full scan cycle across all symbols. Returns confluence."""
        self.cycle += 1
        results: dict[str, list[ConfluenceReport]] = {}

        for symbol in self.symbols:
            try:
                base_df = self.fetch_fn(symbol)
            except Exception as exc:
                print(f"[alive] fetch error {symbol}: {exc}")
                continue

            last_price = float(base_df["close"].iloc[-1])
            divergences = self.scanner.scan(base_df, symbol)

            for d in divergences:
                key = _divergence_key(d)
                if key in self.tracked:
                    t = self.tracked[key]
                    t.divergence = d  # refresh score/freshness
                    t.last_seen_cycle = self.cycle
                else:
                    target, invalidation = self._levels_for(d, base_df)
                    t = TrackedDivergence(
                        key=key,
                        divergence=d,
                        target_price=target,
                        invalidation_price=invalidation,
                        last_seen_cycle=self.cycle,
                    )
                    self.tracked[key] = t
                    self._emit(t.to_event("NEW_SIGNAL"))

            # advance lifecycles for every tracked signal of this symbol
            for t in list(self.tracked.values()):
                if t.divergence.symbol == symbol:
                    self._update_lifecycle(t, last_price)

            reports = self.scanner.confluence(
                [
                    t.divergence
                    for t in self.tracked.values()
                    if t.divergence.symbol == symbol
                    and t.state in ("CONFIRMED", "PLAYING_OUT")
                ],
                symbol,
            )
            results[symbol] = reports

            for r in reports:
                if r.strength in ("STRONG", "EXTREME"):
                    self._emit(
                        {
                            "event": "CONFLUENCE",
                            "timestamp": datetime.now(timezone.utc).isoformat(timespec="seconds"),
                            "symbol": symbol,
                            "direction": r.direction,
                            "signal": f"confluence_{r.direction}",
                            "score": r.confluence_score,
                            "grade": r.strength,
                            "timeframes": r.timeframes,
                            "indicators": r.indicators,
                        }
                    )

        self.last_confluence = results
        return results

    # ------------------------------------------------------------------
    def run_forever(self) -> None:
        """Blocking loop — the engine's life. Ctrl+C to stop."""
        print(
            f"[alive] engine started | symbols={self.symbols} "
            f"| timeframes={self.scanner.timeframes} "
            f"| indicators={self.scanner.indicators} "
            f"| poll={self.poll_seconds}s"
        )
        try:
            while True:
                started = time.time()
                self.scan_once()
                active = sum(
                    1 for t in self.tracked.values() if t.state in ("CONFIRMED", "PLAYING_OUT")
                )
                print(
                    f"[alive] cycle {self.cycle} done in {time.time()-started:.1f}s "
                    f"| tracked={len(self.tracked)} active={active}"
                )
                time.sleep(max(0.0, self.poll_seconds - (time.time() - started)))
        except KeyboardInterrupt:
            print("\n[alive] engine stopped.")
