"""
mtf.py — Multi-timeframe divergence scanning and confluence.

The system never trusts a single timeframe. It scans every configured
timeframe with every configured indicator, then computes a CONFLUENCE
report: how many independent (timeframe, indicator) pairs currently agree
on the same directional signal. A divergence confirmed on 15m, 1h, and 4h
by RSI + MACD simultaneously is a fundamentally different event than a
lone 5m stochastic wiggle — the confluence score expresses that.
"""

from __future__ import annotations

from dataclasses import dataclass, field

import pandas as pd

from .divergence import Divergence, detect_divergences
from .indicators import compute_indicator
from .scoring import score_divergence

# pandas resample rules for common timeframes
TIMEFRAME_RULES: dict[str, str] = {
    "1m": "1min",
    "3m": "3min",
    "5m": "5min",
    "15m": "15min",
    "30m": "30min",
    "1h": "1h",
    "2h": "2h",
    "4h": "4h",
    "6h": "6h",
    "8h": "8h",
    "12h": "12h",
    "1d": "1D",
    "3d": "3D",
    "1w": "1W",
}

TIMEFRAME_MINUTES: dict[str, int] = {
    "1m": 1, "3m": 3, "5m": 5, "15m": 15, "30m": 30,
    "1h": 60, "2h": 120, "4h": 240, "6h": 360, "8h": 480,
    "12h": 720, "1d": 1440, "3d": 4320, "1w": 10080,
}


def resample_ohlcv(df: pd.DataFrame, timeframe: str) -> pd.DataFrame:
    """Resample a base OHLCV frame (DatetimeIndex) up to a higher timeframe."""
    rule = TIMEFRAME_RULES[timeframe]
    out = pd.DataFrame(
        {
            "open": df["open"].resample(rule).first(),
            "high": df["high"].resample(rule).max(),
            "low": df["low"].resample(rule).min(),
            "close": df["close"].resample(rule).last(),
            "volume": df["volume"].resample(rule).sum(),
        }
    ).dropna(subset=["open", "high", "low", "close"])
    return out


@dataclass
class ConfluenceReport:
    symbol: str
    direction: str                       # "bullish" | "bearish"
    divergences: list[Divergence]
    timeframes: list[str] = field(default_factory=list)
    indicators: list[str] = field(default_factory=list)
    confluence_score: float = 0.0        # 0-100
    strength: str = ""                   # WEAK / MODERATE / STRONG / EXTREME

    def to_dict(self) -> dict:
        return {
            "symbol": self.symbol,
            "direction": self.direction,
            "confluence_score": round(self.confluence_score, 2),
            "strength": self.strength,
            "timeframes": self.timeframes,
            "indicators": self.indicators,
            "signals": [d.to_dict() for d in self.divergences],
        }


class MultiTimeframeScanner:
    """Runs the full divergence engine across every timeframe x indicator."""

    def __init__(
        self,
        timeframes: list[str],
        indicators: list[str],
        indicator_params: dict[str, dict] | None = None,
        min_score: float = 40.0,
        max_signal_age_bars: int = 12,
        pivot_left: int = 5,
        pivot_right: int = 5,
        include_hidden: bool = True,
    ) -> None:
        unknown = set(timeframes) - set(TIMEFRAME_RULES)
        if unknown:
            raise ValueError(f"Unknown timeframes: {unknown}")
        self.timeframes = sorted(timeframes, key=lambda t: TIMEFRAME_MINUTES[t])
        self.indicators = indicators
        self.indicator_params = indicator_params or {}
        self.min_score = min_score
        self.max_signal_age_bars = max_signal_age_bars
        self.pivot_left = pivot_left
        self.pivot_right = pivot_right
        self.include_hidden = include_hidden

    # ------------------------------------------------------------------
    def scan_timeframe(
        self, df_tf: pd.DataFrame, symbol: str, timeframe: str
    ) -> list[Divergence]:
        found: list[Divergence] = []
        for ind in self.indicators:
            try:
                osc = compute_indicator(df_tf, ind, self.indicator_params.get(ind))
            except Exception:
                continue
            divs = detect_divergences(
                df_tf,
                osc,
                indicator_name=ind,
                symbol=symbol,
                timeframe=timeframe,
                pivot_left=self.pivot_left,
                pivot_right=self.pivot_right,
                include_hidden=self.include_hidden,
            )
            for d in divs:
                score_divergence(d, df_tf, osc)
                age = len(df_tf) - 1 - d.confirmed_index
                if d.score >= self.min_score and age <= self.max_signal_age_bars:
                    found.append(d)
        return found

    # ------------------------------------------------------------------
    def scan(self, base_df: pd.DataFrame, symbol: str) -> list[Divergence]:
        """Scan every timeframe. base_df must be the LOWEST timeframe data
        with a DatetimeIndex; higher TFs are resampled from it."""
        all_divs: list[Divergence] = []
        for tf in self.timeframes:
            df_tf = resample_ohlcv(base_df, tf)
            if len(df_tf) < 60:  # not enough bars to be meaningful
                continue
            all_divs.extend(self.scan_timeframe(df_tf, symbol, tf))
        return all_divs

    # ------------------------------------------------------------------
    def confluence(self, divergences: list[Divergence], symbol: str) -> list[ConfluenceReport]:
        """Group active divergences by direction and compute confluence."""
        reports: list[ConfluenceReport] = []
        for direction in ("bullish", "bearish"):
            group = [d for d in divergences if d.direction == direction]
            if not group:
                continue
            tfs = sorted({d.timeframe for d in group}, key=lambda t: TIMEFRAME_MINUTES[t])
            inds = sorted({d.indicator for d in group})

            # Confluence: weighted by number of TFs, number of indicators,
            # average quality, and higher-timeframe participation.
            avg_quality = sum(d.score for d in group) / len(group)
            tf_weight = min(len(tfs) / max(len(self.timeframes), 1), 1.0)
            ind_weight = min(len(inds) / max(len(self.indicators), 1), 1.0)
            htf_bonus = 0.0
            if self.timeframes:
                highest = self.timeframes[-1]
                if highest in tfs:
                    htf_bonus = 10.0

            conf = min(
                100.0,
                avg_quality * 0.5 + tf_weight * 25.0 + ind_weight * 15.0 + htf_bonus,
            )
            strength = (
                "EXTREME" if conf >= 80 else
                "STRONG" if conf >= 65 else
                "MODERATE" if conf >= 45 else
                "WEAK"
            )
            reports.append(
                ConfluenceReport(
                    symbol=symbol,
                    direction=direction,
                    divergences=sorted(group, key=lambda d: -d.score),
                    timeframes=tfs,
                    indicators=inds,
                    confluence_score=conf,
                    strength=strength,
                )
            )
        return sorted(reports, key=lambda r: -r.confluence_score)
