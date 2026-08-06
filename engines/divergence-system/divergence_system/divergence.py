"""
divergence.py — Core divergence detection engine.

Detects all four classic divergence types between price and any oscillator:

  Regular Bullish : price makes Lower Low,  oscillator makes Higher Low
  Regular Bearish : price makes Higher High, oscillator makes Lower High
  Hidden  Bullish : price makes Higher Low,  oscillator makes Lower Low
  Hidden  Bearish : price makes Lower High,  oscillator makes Higher High

Design principles for a HIGH-QUALITY detector:
  * Only confirmed (non-repainting) pivots are used.
  * Price pivots and oscillator pivots are aligned with a small tolerance
    window instead of requiring the exact same bar.
  * Multiple consecutive pivot pairs are scanned (not just the last two),
    so nested/older divergences are found too.
  * A minimum and maximum pivot distance filters out noise and stale setups.
  * Slope-consistency check: the oscillator between the two pivots must not
    fully break the divergence line (line-cut validation).
"""

from __future__ import annotations

from dataclasses import dataclass, field

import numpy as np
import pandas as pd

from .pivots import Pivot, find_pivots, nearest_pivot


@dataclass
class Divergence:
    kind: str                 # "regular" | "hidden"
    direction: str            # "bullish" | "bearish"
    indicator: str            # e.g. "rsi"
    timeframe: str            # e.g. "1h"
    symbol: str

    price_start: float
    price_end: float
    osc_start: float
    osc_end: float

    start_time: pd.Timestamp
    end_time: pd.Timestamp
    start_index: int
    end_index: int
    confirmed_index: int      # bar where the signal became actionable

    bars_between: int = 0
    score: float = 0.0        # 0-100 quality score, filled by scoring module
    grade: str = ""           # A+/A/B/C
    meta: dict = field(default_factory=dict)

    @property
    def signal(self) -> str:
        return f"{self.kind}_{self.direction}"

    def to_dict(self) -> dict:
        return {
            "symbol": self.symbol,
            "timeframe": self.timeframe,
            "indicator": self.indicator,
            "type": self.kind,
            "direction": self.direction,
            "signal": self.signal,
            "price_start": round(self.price_start, 8),
            "price_end": round(self.price_end, 8),
            "osc_start": round(self.osc_start, 6),
            "osc_end": round(self.osc_end, 6),
            "start_time": str(self.start_time),
            "end_time": str(self.end_time),
            "bars_between": self.bars_between,
            "score": round(self.score, 2),
            "grade": self.grade,
        }


def _line_cut_ok(
    series: pd.Series, i0: int, i1: int, v0: float, v1: float, tolerance: float = 0.10
) -> bool:
    """Validate that the series between the two pivots does not decisively
    break the straight line joining them. A small tolerance (fraction of the
    line range) is allowed. This kills 'divergences' drawn through the data.
    """
    if i1 - i0 < 2:
        return True
    xs = np.arange(i0, i1 + 1)
    line = v0 + (v1 - v0) * (xs - i0) / (i1 - i0)
    seg = series.iloc[i0 : i1 + 1].to_numpy(dtype=float)
    rng = max(abs(v1 - v0), np.nanstd(seg), 1e-12)
    # Count how far values pierce beyond the line
    overshoot = np.nanmax(np.abs(seg - line)) if len(seg) else 0.0
    breaches = np.abs(seg - line) > rng * (1.0 + tolerance)
    return not breaches.any() or overshoot < rng * 1.5


def detect_divergences(
    df: pd.DataFrame,
    osc: pd.Series,
    indicator_name: str,
    symbol: str = "",
    timeframe: str = "",
    pivot_left: int = 5,
    pivot_right: int = 5,
    min_bars_between: int = 5,
    max_bars_between: int = 60,
    align_tolerance: int = 3,
    max_pairs_back: int = 4,
    include_hidden: bool = True,
) -> list[Divergence]:
    """Scan an OHLCV frame + oscillator for all divergence types.

    Returns a list of Divergence objects (unscored — see scoring.py).
    """
    results: list[Divergence] = []

    price_highs = find_pivots(df["high"], pivot_left, pivot_right, "high")
    price_lows = find_pivots(df["low"], pivot_left, pivot_right, "low")
    osc_highs = find_pivots(osc, pivot_left, pivot_right, "high")
    osc_lows = find_pivots(osc, pivot_left, pivot_right, "low")

    def scan(
        p_pivots: list[Pivot],
        o_pivots: list[Pivot],
        checker,
        kind: str,
        direction: str,
    ) -> None:
        # examine the last `max_pairs_back` adjacent pivot pairs
        for j in range(len(p_pivots) - 1, 0, -1):
            if len(p_pivots) - 1 - j >= max_pairs_back:
                break
            p1, p2 = p_pivots[j - 1], p_pivots[j]
            bars = p2.index - p1.index
            if bars < min_bars_between or bars > max_bars_between:
                continue

            o1 = nearest_pivot(o_pivots, p1.index, align_tolerance)
            o2 = nearest_pivot(o_pivots, p2.index, align_tolerance)
            if o1 is None or o2 is None or o1.index == o2.index:
                continue

            if not checker(p1.price, p2.price, o1.price, o2.price):
                continue

            i0, i1 = sorted((o1.index, o2.index))
            if not _line_cut_ok(osc, i0, i1, osc.iloc[i0], osc.iloc[i1]):
                continue

            results.append(
                Divergence(
                    kind=kind,
                    direction=direction,
                    indicator=indicator_name,
                    timeframe=timeframe,
                    symbol=symbol,
                    price_start=p1.price,
                    price_end=p2.price,
                    osc_start=o1.price,
                    osc_end=o2.price,
                    start_time=p1.timestamp,
                    end_time=p2.timestamp,
                    start_index=p1.index,
                    end_index=p2.index,
                    confirmed_index=max(p2.confirmed_at, o2.confirmed_at),
                    bars_between=bars,
                )
            )

    # Regular bullish: price LL, osc HL  (on lows)
    scan(price_lows, osc_lows, lambda pp1, pp2, oo1, oo2: pp2 < pp1 and oo2 > oo1, "regular", "bullish")
    # Regular bearish: price HH, osc LH  (on highs)
    scan(price_highs, osc_highs, lambda pp1, pp2, oo1, oo2: pp2 > pp1 and oo2 < oo1, "regular", "bearish")

    if include_hidden:
        # Hidden bullish: price HL, osc LL (on lows)
        scan(price_lows, osc_lows, lambda pp1, pp2, oo1, oo2: pp2 > pp1 and oo2 < oo1, "hidden", "bullish")
        # Hidden bearish: price LH, osc HH (on highs)
        scan(price_highs, osc_highs, lambda pp1, pp2, oo1, oo2: pp2 < pp1 and oo2 > oo1, "hidden", "bearish")

    return results
