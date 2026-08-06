"""
data.py — Market data providers.

Three providers are included:
  * YFinanceProvider — stocks / ETFs / FX / crypto via yfinance (free)
  * CCXTProvider     — any crypto exchange supported by ccxt (Binance, etc.)
  * SyntheticProvider — deterministic random-walk data for offline testing

All providers return a normalized OHLCV DataFrame:
  DatetimeIndex (UTC), columns: open, high, low, close, volume
"""

from __future__ import annotations

import numpy as np
import pandas as pd

REQUIRED_COLS = ["open", "high", "low", "close", "volume"]


def _normalize(df: pd.DataFrame) -> pd.DataFrame:
    df = df.copy()
    df.columns = [str(c).lower() for c in df.columns]
    for col in REQUIRED_COLS:
        if col not in df.columns:
            if col == "volume":
                df["volume"] = 0.0
            else:
                raise ValueError(f"Missing OHLCV column: {col}")
    df = df[REQUIRED_COLS].astype(float)
    if not isinstance(df.index, pd.DatetimeIndex):
        df.index = pd.to_datetime(df.index, utc=True)
    if df.index.tz is None:
        df.index = df.index.tz_localize("UTC")
    return df.sort_index()


class YFinanceProvider:
    """Fetch data via yfinance. `pip install yfinance`."""

    def fetch(self, symbol: str, interval: str = "5m", lookback: str = "30d") -> pd.DataFrame:
        import yfinance as yf

        raw = yf.download(
            symbol,
            interval=interval,
            period=lookback,
            auto_adjust=True,
            progress=False,
        )
        if raw is None or raw.empty:
            raise RuntimeError(f"No data returned for {symbol}")
        if isinstance(raw.columns, pd.MultiIndex):
            raw.columns = raw.columns.get_level_values(0)
        return _normalize(raw)


class CCXTProvider:
    """Fetch crypto data via ccxt. `pip install ccxt`."""

    def __init__(self, exchange: str = "binance") -> None:
        import ccxt

        self.client = getattr(ccxt, exchange)({"enableRateLimit": True})

    def fetch(self, symbol: str, interval: str = "5m", limit: int = 1500) -> pd.DataFrame:
        ohlcv = self.client.fetch_ohlcv(symbol, timeframe=interval, limit=limit)
        df = pd.DataFrame(
            ohlcv, columns=["ts", "open", "high", "low", "close", "volume"]
        )
        df.index = pd.to_datetime(df.pop("ts"), unit="ms", utc=True)
        return _normalize(df)


class SyntheticProvider:
    """Deterministic synthetic OHLCV — lets you run the whole system offline."""

    def fetch(
        self,
        symbol: str = "SYN/USD",
        interval: str = "5m",
        bars: int = 4000,
        seed: int = 42,
        start_price: float = 100.0,
    ) -> pd.DataFrame:
        rng = np.random.default_rng(seed + sum(map(ord, symbol)))
        minutes = {"1m": 1, "5m": 5, "15m": 15, "1h": 60}.get(interval, 5)

        # Random walk with regime shifts and cyclical component so real
        # divergences appear naturally.
        t = np.arange(bars)
        drift = np.cumsum(rng.normal(0, 0.0008, bars))
        cycle = 0.03 * np.sin(t / 90.0) + 0.015 * np.sin(t / 37.0)
        noise = rng.normal(0, 0.004, bars)
        log_price = np.log(start_price) + drift + cycle + noise
        close = np.exp(log_price)

        spread = np.abs(rng.normal(0, 0.003, bars)) * close
        open_ = np.concatenate([[close[0]], close[:-1]])
        high = np.maximum(open_, close) + spread
        low = np.minimum(open_, close) - spread
        volume = rng.lognormal(10, 0.5, bars) * (1 + 3 * np.abs(noise))

        index = pd.date_range(
            end=pd.Timestamp.now(tz="UTC").floor(f"{minutes}min"),
            periods=bars,
            freq=f"{minutes}min",
        )
        return _normalize(
            pd.DataFrame(
                {"open": open_, "high": high, "low": low, "close": close, "volume": volume},
                index=index,
            )
        )


def get_provider(name: str, **kwargs):
    name = name.lower()
    if name in ("yfinance", "yahoo"):
        return YFinanceProvider()
    if name == "ccxt":
        return CCXTProvider(**kwargs)
    if name in ("synthetic", "demo"):
        return SyntheticProvider()
    raise ValueError(f"Unknown provider: {name}")
