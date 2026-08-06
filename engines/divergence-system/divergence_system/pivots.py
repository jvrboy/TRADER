"""
pivots.py — Swing high/low (pivot) detection with confirmation semantics.

A pivot high at index i requires `left` bars strictly lower before it and
`right` bars strictly lower after it. The pivot is only *confirmed* once the
`right` bars have printed — the confirmation index is tracked so the live
engine never acts on unconfirmed (repainting) pivots.
"""

from __future__ import annotations

from dataclasses import dataclass

import numpy as np
import pandas as pd


@dataclass(frozen=True)
class Pivot:
    index: int          # positional index of the pivot bar
    timestamp: pd.Timestamp
    price: float        # value at the pivot (price or oscillator)
    kind: str           # "high" | "low"
    confirmed_at: int   # positional index where pivot became confirmed


def find_pivots(
    series: pd.Series,
    left: int = 5,
    right: int = 5,
    kind: str = "high",
) -> list[Pivot]:
    """Detect confirmed fractal pivots in a series.

    Parameters
    ----------
    series : values to scan (e.g. df['high'], df['low'], or an oscillator)
    left   : bars to the left that must be strictly beyond
    right  : bars to the right that must be strictly beyond (confirmation lag)
    kind   : "high" for swing highs, "low" for swing lows
    """
    values = series.to_numpy(dtype=float)
    n = len(values)
    pivots: list[Pivot] = []

    for i in range(left, n - right):
        v = values[i]
        if np.isnan(v):
            continue
        window_left = values[i - left : i]
        window_right = values[i + 1 : i + 1 + right]
        if np.isnan(window_left).any() or np.isnan(window_right).any():
            continue

        if kind == "high":
            if v > window_left.max() and v >= window_right.max():
                pivots.append(
                    Pivot(
                        index=i,
                        timestamp=series.index[i],
                        price=float(v),
                        kind="high",
                        confirmed_at=i + right,
                    )
                )
        else:
            if v < window_left.min() and v <= window_right.min():
                pivots.append(
                    Pivot(
                        index=i,
                        timestamp=series.index[i],
                        price=float(v),
                        kind="low",
                        confirmed_at=i + right,
                    )
                )

    return pivots


def last_n_pivots(pivots: list[Pivot], n: int = 2, before_index: int | None = None) -> list[Pivot]:
    """Return the last n confirmed pivots, optionally only those confirmed
    at or before `before_index` (prevents lookahead in backtests)."""
    if before_index is not None:
        pivots = [p for p in pivots if p.confirmed_at <= before_index]
    return pivots[-n:]


def nearest_pivot(
    pivots: list[Pivot], index: int, max_distance: int = 3
) -> Pivot | None:
    """Find the pivot closest to a positional index within max_distance bars.

    Used to align price pivots with oscillator pivots — the two rarely land
    on the exact same bar, so a small alignment tolerance is essential for
    a high-quality divergence system.
    """
    best: Pivot | None = None
    best_dist = max_distance + 1
    for p in pivots:
        d = abs(p.index - index)
        if d < best_dist:
            best = p
            best_dist = d
    return best
