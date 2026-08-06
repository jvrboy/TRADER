"""
config.py — Central configuration with sane, battle-tested defaults.

Override anything via a JSON file passed to `main.py --config my.json`.
"""

from __future__ import annotations

import json
from dataclasses import dataclass, field


@dataclass
class Config:
    # Data
    provider: str = "synthetic"          # synthetic | yfinance | ccxt
    exchange: str = "binance"            # for ccxt
    symbols: list[str] = field(default_factory=lambda: ["BTC/USDT"])
    base_interval: str = "5m"            # lowest timeframe fetched
    lookback: str = "30d"                # for yfinance
    ccxt_limit: int = 1500

    # Multi-timeframe
    timeframes: list[str] = field(
        default_factory=lambda: ["5m", "15m", "1h", "4h"]
    )

    # Indicators
    indicators: list[str] = field(
        default_factory=lambda: ["rsi", "macd", "macd_hist", "stochastic", "cci", "mfi", "obv"]
    )
    indicator_params: dict = field(default_factory=dict)

    # Detection
    pivot_left: int = 5
    pivot_right: int = 5
    include_hidden: bool = True
    min_score: float = 45.0
    max_signal_age_bars: int = 12

    # Alive engine / agent colony
    poll_seconds: float = 60.0
    target_atr_multiple: float = 2.0
    expiry_bars: int = 40
    jsonl_log: str = "divergence_events.jsonl"
    alerts_file: str | None = "divergence_alerts.jsonl"
    webhook_url: str = ""

    # Replay (agent colony backtest)
    replay_warmup: int = 1000
    replay_step: int = 12

    @classmethod
    def from_json(cls, path: str) -> "Config":
        with open(path, encoding="utf-8") as f:
            data = json.load(f)
        cfg = cls()
        for k, v in data.items():
            if hasattr(cfg, k):
                setattr(cfg, k, v)
            else:
                print(f"[config] ignoring unknown key: {k}")
        return cfg
