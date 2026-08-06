"""
indicators.py — Pure-pandas/numpy technical indicator library.

Every indicator returns a pandas Series (or DataFrame for multi-line
indicators) aligned to the input index. No TA-Lib dependency required.
"""

from __future__ import annotations

import numpy as np
import pandas as pd


# ----------------------------------------------------------------------------
# Moving averages
# ----------------------------------------------------------------------------

def ema(series: pd.Series, period: int) -> pd.Series:
    return series.ewm(span=period, adjust=False).mean()


def sma(series: pd.Series, period: int) -> pd.Series:
    return series.rolling(period).mean()


def wilder_smooth(series: pd.Series, period: int) -> pd.Series:
    """Wilder's smoothing (RMA) as used by RSI/ATR."""
    return series.ewm(alpha=1.0 / period, adjust=False).mean()


# ----------------------------------------------------------------------------
# Oscillators
# ----------------------------------------------------------------------------

def rsi(close: pd.Series, period: int = 14) -> pd.Series:
    delta = close.diff()
    gain = wilder_smooth(delta.clip(lower=0.0), period)
    loss = wilder_smooth((-delta).clip(lower=0.0), period)
    rs = gain / loss.replace(0.0, np.nan)
    out = 100.0 - (100.0 / (1.0 + rs))
    return out.fillna(50.0).rename("rsi")


def macd(
    close: pd.Series,
    fast: int = 12,
    slow: int = 26,
    signal: int = 9,
) -> pd.DataFrame:
    macd_line = ema(close, fast) - ema(close, slow)
    signal_line = ema(macd_line, signal)
    hist = macd_line - signal_line
    return pd.DataFrame(
        {"macd": macd_line, "signal": signal_line, "hist": hist}
    )


def stochastic(
    high: pd.Series,
    low: pd.Series,
    close: pd.Series,
    k_period: int = 14,
    d_period: int = 3,
    smooth_k: int = 3,
) -> pd.DataFrame:
    lowest = low.rolling(k_period).min()
    highest = high.rolling(k_period).max()
    raw_k = 100.0 * (close - lowest) / (highest - lowest).replace(0.0, np.nan)
    k = raw_k.rolling(smooth_k).mean()
    d = k.rolling(d_period).mean()
    return pd.DataFrame({"stoch_k": k, "stoch_d": d})


def cci(
    high: pd.Series, low: pd.Series, close: pd.Series, period: int = 20
) -> pd.Series:
    tp = (high + low + close) / 3.0
    ma = tp.rolling(period).mean()
    mad = tp.rolling(period).apply(
        lambda x: np.mean(np.abs(x - x.mean())), raw=True
    )
    return ((tp - ma) / (0.015 * mad.replace(0.0, np.nan))).rename("cci")


def mfi(
    high: pd.Series,
    low: pd.Series,
    close: pd.Series,
    volume: pd.Series,
    period: int = 14,
) -> pd.Series:
    tp = (high + low + close) / 3.0
    raw_flow = tp * volume
    direction = tp.diff()
    pos = raw_flow.where(direction > 0, 0.0).rolling(period).sum()
    neg = raw_flow.where(direction < 0, 0.0).rolling(period).sum()
    ratio = pos / neg.replace(0.0, np.nan)
    return (100.0 - 100.0 / (1.0 + ratio)).rename("mfi")


def obv(close: pd.Series, volume: pd.Series) -> pd.Series:
    direction = np.sign(close.diff()).fillna(0.0)
    return (direction * volume).cumsum().rename("obv")


def momentum(close: pd.Series, period: int = 10) -> pd.Series:
    return close.diff(period).rename("momentum")


def roc(close: pd.Series, period: int = 12) -> pd.Series:
    return (close.pct_change(period) * 100.0).rename("roc")


def williams_r(
    high: pd.Series, low: pd.Series, close: pd.Series, period: int = 14
) -> pd.Series:
    highest = high.rolling(period).max()
    lowest = low.rolling(period).min()
    out = -100.0 * (highest - close) / (highest - lowest).replace(0.0, np.nan)
    return out.rename("williams_r")


def awesome_oscillator(high: pd.Series, low: pd.Series) -> pd.Series:
    mid = (high + low) / 2.0
    return (sma(mid, 5) - sma(mid, 34)).rename("ao")


def atr(
    high: pd.Series, low: pd.Series, close: pd.Series, period: int = 14
) -> pd.Series:
    prev_close = close.shift(1)
    tr = pd.concat(
        [
            high - low,
            (high - prev_close).abs(),
            (low - prev_close).abs(),
        ],
        axis=1,
    ).max(axis=1)
    return wilder_smooth(tr, period).rename("atr")


# ----------------------------------------------------------------------------
# Registry: name -> callable(df, **params) -> Series
# Each entry computes the oscillator line used for divergence analysis.
# ----------------------------------------------------------------------------

def compute_indicator(df: pd.DataFrame, name: str, params: dict | None = None) -> pd.Series:
    """Compute a single divergence-ready oscillator series from an OHLCV frame.

    df must have columns: open, high, low, close, volume.
    """
    p = params or {}
    name = name.lower()

    if name == "rsi":
        return rsi(df["close"], p.get("period", 14))
    if name == "macd":
        return macd(df["close"], p.get("fast", 12), p.get("slow", 26), p.get("signal", 9))["macd"]
    if name == "macd_hist":
        return macd(df["close"], p.get("fast", 12), p.get("slow", 26), p.get("signal", 9))["hist"]
    if name == "stochastic":
        return stochastic(df["high"], df["low"], df["close"], p.get("k_period", 14), p.get("d_period", 3), p.get("smooth_k", 3))["stoch_k"]
    if name == "cci":
        return cci(df["high"], df["low"], df["close"], p.get("period", 20))
    if name == "mfi":
        return mfi(df["high"], df["low"], df["close"], df["volume"], p.get("period", 14))
    if name == "obv":
        return obv(df["close"], df["volume"])
    if name == "momentum":
        return momentum(df["close"], p.get("period", 10))
    if name == "roc":
        return roc(df["close"], p.get("period", 12))
    if name == "williams_r":
        return williams_r(df["high"], df["low"], df["close"], p.get("period", 14))
    if name == "ao":
        return awesome_oscillator(df["high"], df["low"])

    raise ValueError(f"Unknown indicator: {name}")


AVAILABLE_INDICATORS = [
    "rsi",
    "macd",
    "macd_hist",
    "stochastic",
    "cci",
    "mfi",
    "obv",
    "momentum",
    "roc",
    "williams_r",
    "ao",
]
