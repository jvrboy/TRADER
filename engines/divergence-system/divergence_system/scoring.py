"""
scoring.py — Divergence quality scoring (0-100) and grading.

A divergence is not a binary event: its strength depends on the magnitude of
the price/oscillator disagreement, the freshness of the signal, the zone the
oscillator is in (overbought/oversold), and trend/volume context. This module
converts each Divergence into an objective score so signals can be ranked
and filtered.

Score components (weights configurable):
  * angle_delta   — how strongly price and oscillator slopes disagree
  * osc_zone      — bonus if oscillator is in an extreme zone
  * freshness     — how recently the signal confirmed
  * pivot_span    — sweet-spot number of bars between pivots
  * trend_context — regular divergences against overextended trends score
                    higher; hidden divergences WITH the trend score higher
  * volume        — rising volume into the second pivot adds conviction
"""

from __future__ import annotations

import numpy as np
import pandas as pd

from .divergence import Divergence
from .indicators import atr, ema

# Oscillator extreme zones: (oversold_below, overbought_above). None = no zone concept.
OSC_ZONES: dict[str, tuple[float, float] | None] = {
    "rsi": (30.0, 70.0),
    "stochastic": (20.0, 80.0),
    "mfi": (20.0, 80.0),
    "williams_r": (-80.0, -20.0),
    "cci": (-100.0, 100.0),
    "macd": None,
    "macd_hist": None,
    "obv": None,
    "momentum": None,
    "roc": None,
    "ao": None,
}

DEFAULT_WEIGHTS = {
    "angle_delta": 30.0,
    "osc_zone": 20.0,
    "freshness": 15.0,
    "pivot_span": 10.0,
    "trend_context": 15.0,
    "volume": 10.0,
}


def _norm_slope(v0: float, v1: float, bars: int, scale: float) -> float:
    """Slope normalized by a volatility scale so different symbols compare."""
    if bars <= 0 or scale <= 0:
        return 0.0
    return (v1 - v0) / bars / scale


def score_divergence(
    div: Divergence,
    df: pd.DataFrame,
    osc: pd.Series,
    weights: dict | None = None,
) -> Divergence:
    """Fill div.score (0-100) and div.grade in place; returns the same object."""
    w = {**DEFAULT_WEIGHTS, **(weights or {})}
    total_weight = sum(w.values())
    score = 0.0

    n = len(df)
    bars = max(div.bars_between, 1)

    # --- 1. Angle delta: normalized slope disagreement -----------------------
    atr_series = atr(df["high"], df["low"], df["close"], 14)
    price_scale = float(atr_series.iloc[min(div.end_index, n - 1)] or 1e-9)
    osc_scale = float(np.nanstd(osc.tail(200))) or 1e-9

    price_slope = _norm_slope(div.price_start, div.price_end, bars, price_scale)
    osc_slope = _norm_slope(div.osc_start, div.osc_end, bars, osc_scale)
    disagreement = abs(price_slope - osc_slope)
    score += w["angle_delta"] * float(np.tanh(disagreement * 2.0))

    # --- 2. Oscillator zone ---------------------------------------------------
    zone = OSC_ZONES.get(div.indicator)
    zone_score = 0.5  # neutral default for zoneless oscillators
    if zone is not None:
        oversold, overbought = zone
        v = div.osc_end
        if div.direction == "bullish":
            if v <= oversold:
                zone_score = 1.0
            elif v <= oversold + (overbought - oversold) * 0.25:
                zone_score = 0.7
            else:
                zone_score = 0.25
        else:
            if v >= overbought:
                zone_score = 1.0
            elif v >= overbought - (overbought - oversold) * 0.25:
                zone_score = 0.7
            else:
                zone_score = 0.25
    score += w["osc_zone"] * zone_score

    # --- 3. Freshness -----------------------------------------------------------
    age = max(n - 1 - div.confirmed_index, 0)
    freshness = float(np.exp(-age / 10.0))  # decays; ~0 after ~30 bars
    score += w["freshness"] * freshness

    # --- 4. Pivot span sweet spot (10-40 bars ideal) ---------------------------
    if 10 <= bars <= 40:
        span_score = 1.0
    elif bars < 10:
        span_score = bars / 10.0
    else:
        span_score = max(0.0, 1.0 - (bars - 40) / 40.0)
    score += w["pivot_span"] * span_score

    # --- 5. Trend context -------------------------------------------------------
    trend = ema(df["close"], 50)
    idx = min(div.end_index, n - 1)
    above = bool(df["close"].iloc[idx] > trend.iloc[idx]) if not np.isnan(trend.iloc[idx]) else True
    if div.kind == "regular":
        # counter-trend reversal signal: stronger when against the move
        ctx = 1.0 if (div.direction == "bullish") != above else 0.5
    else:
        # hidden = continuation: stronger when WITH the trend
        ctx = 1.0 if (div.direction == "bullish") == above else 0.4
    score += w["trend_context"] * ctx

    # --- 6. Volume confirmation --------------------------------------------------
    vol_score = 0.5
    if "volume" in df.columns and df["volume"].sum() > 0:
        recent = df["volume"].iloc[max(idx - 5, 0) : idx + 1].mean()
        base = df["volume"].iloc[max(idx - 30, 0) : idx + 1].mean()
        if base > 0:
            ratio = recent / base
            vol_score = float(np.clip((ratio - 0.6) / 0.9, 0.0, 1.0))
    score += w["volume"] * vol_score

    div.score = round(100.0 * score / total_weight, 2)
    div.grade = grade_for(div.score)
    div.meta.update(
        {
            "zone_score": round(zone_score, 2),
            "freshness": round(freshness, 3),
            "trend_alignment": ctx,
            "volume_score": round(vol_score, 2),
            "age_bars": age,
        }
    )
    return div


def grade_for(score: float) -> str:
    if score >= 80:
        return "A+"
    if score >= 68:
        return "A"
    if score >= 55:
        return "B"
    if score >= 40:
        return "C"
    return "D"
