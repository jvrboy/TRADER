"""
lifecycle.py — LivingDivergence: a divergence as a stateful, mortal entity.

A detected divergence is not a one-off alert — it is BORN, it LIVES while
price decides its fate, and it DIES in exactly one of three ways:

  COMPLETED   — price reached the ATR-based target in the signal direction
  INVALIDATED — price decisively broke through the divergence pivot
  EXPIRED     — too many bars passed with no resolution

While alive it also reports interim state changes ("halfway" toward target)
and tracks its maximum favorable / adverse excursion, so the colony can
accumulate objective per-indicator, per-timeframe reliability statistics.
"""

from __future__ import annotations

import itertools
import time
from dataclasses import dataclass, field

from .divergence import Divergence

_id_counter = itertools.count(1)

ALIVE_STATES = ("born", "active", "halfway")
DEAD_STATES = ("completed", "invalidated", "expired")


@dataclass
class LivingDivergence:
    div: Divergence
    entry_price: float
    atr: float
    target_atr_multiple: float = 2.0
    invalidation_atr_buffer: float = 0.25
    expiry_bars: int = 40

    id: int = field(default_factory=lambda: next(_id_counter))
    state: str = "born"
    bars_alive: int = 0
    born_ts: float = field(default_factory=time.time)
    died_ts: float | None = None

    target_price: float = 0.0
    invalidation_price: float = 0.0
    max_favorable: float = 0.0   # best excursion toward target (price units)
    max_adverse: float = 0.0     # worst excursion against the signal

    def __post_init__(self) -> None:
        atr = max(self.atr, 1e-12)
        if self.div.direction == "bullish":
            self.target_price = self.entry_price + atr * self.target_atr_multiple
            self.invalidation_price = (
                min(self.div.price_end, self.div.price_start) - atr * self.invalidation_atr_buffer
            )
        else:
            self.target_price = self.entry_price - atr * self.target_atr_multiple
            self.invalidation_price = (
                max(self.div.price_end, self.div.price_start) + atr * self.invalidation_atr_buffer
            )

    # ------------------------------------------------------------------
    @property
    def alive(self) -> bool:
        return self.state in ALIVE_STATES

    @property
    def key(self) -> str:
        d = self.div
        return f"{d.symbol}|{d.timeframe}|{d.indicator}|{d.signal}|{d.end_time}"

    @property
    def label(self) -> str:
        d = self.div
        return f"{d.indicator}@{d.timeframe}"

    # ------------------------------------------------------------------
    def update(self, high: float, low: float, close: float) -> str | None:
        """Feed one closed bar. Returns the new state if it changed, else None."""
        if not self.alive:
            return None
        self.bars_alive += 1

        bullish = self.div.direction == "bullish"
        favorable = (high - self.entry_price) if bullish else (self.entry_price - low)
        adverse = (self.entry_price - low) if bullish else (high - self.entry_price)
        self.max_favorable = max(self.max_favorable, favorable)
        self.max_adverse = max(self.max_adverse, adverse)

        # -- death checks (invalidation wins ties: risk first) -------------
        if bullish and low <= self.invalidation_price:
            return self._die("invalidated")
        if not bullish and high >= self.invalidation_price:
            return self._die("invalidated")

        if bullish and high >= self.target_price:
            return self._die("completed")
        if not bullish and low <= self.target_price:
            return self._die("completed")

        if self.bars_alive >= self.expiry_bars:
            return self._die("expired")

        # -- interim state transitions --------------------------------------
        if self.state == "born":
            self.state = "active"
            return "active"

        span = abs(self.target_price - self.entry_price)
        progressed = (
            (close - self.entry_price) if bullish else (self.entry_price - close)
        )
        if self.state == "active" and span > 0 and progressed >= span * 0.5:
            self.state = "halfway"
            return "halfway"
        return None

    def _die(self, how: str) -> str:
        self.state = how
        self.died_ts = time.time()
        return how

    # ------------------------------------------------------------------
    def to_dict(self) -> dict:
        return {
            "id": self.id,
            "state": self.state,
            "bars_alive": self.bars_alive,
            "entry_price": round(self.entry_price, 8),
            "target_price": round(self.target_price, 8),
            "invalidation_price": round(self.invalidation_price, 8),
            "max_favorable": round(self.max_favorable, 8),
            "max_adverse": round(self.max_adverse, 8),
            "divergence": self.div.to_dict(),
        }


class ReliabilityTracker:
    """Accumulates outcome statistics per indicator@timeframe label."""

    def __init__(self) -> None:
        self._stats: dict[str, dict[str, int]] = {}

    def record(self, ld: LivingDivergence) -> None:
        s = self._stats.setdefault(
            ld.label, {"total": 0, "completed": 0, "invalidated": 0, "expired": 0}
        )
        s["total"] += 1
        if ld.state in s:
            s[ld.state] += 1

    def summary(self) -> dict[str, dict]:
        out: dict[str, dict] = {}
        for label, s in self._stats.items():
            decided = s["completed"] + s["invalidated"]
            win_rate = s["completed"] / decided if decided else 0.0
            out[label] = {**s, "win_rate": win_rate}
        return out
