"""
timeframe_agent.py — TimeframeAgent: owns ONE timeframe of ONE symbol.

Responsibilities:
  * receive the base (lowest-TF) OHLCV frame from `data.updated.<symbol>`
  * resample it up to its own timeframe (closed bars only)
  * publish `data.tf.<symbol>.<tf>` so its indicator sub-agents can scan
  * detect newly CLOSED bars and publish `bar.closed.<symbol>.<tf>` — the
    heartbeat the LifecycleAgent uses to age and resolve living divergences

Each TimeframeAgent spawns one IndicatorSubAgent per indicator, forming
the agent → sub-agent hierarchy:

    orchestrator
      └─ tf[BTC/USDT:1h]
          └─ ind[rsi]@BTC/USDT:1h
          └─ ind[macd]@BTC/USDT:1h
          └─ ...
"""

from __future__ import annotations

import pandas as pd

from ..indicators import atr
from ..mtf import resample_ohlcv
from .base import BaseAgent
from .bus import Message, MessageBus
from .indicator_agent import IndicatorSubAgent


class TimeframeAgent(BaseAgent):
    def __init__(
        self,
        bus: MessageBus,
        symbol: str,
        timeframe: str,
        indicators: list[str],
        indicator_params: dict[str, dict] | None = None,
        pivot_left: int = 5,
        pivot_right: int = 5,
        include_hidden: bool = True,
        min_score: float = 45.0,
        max_signal_age_bars: int = 12,
        parent: BaseAgent | None = None,
    ) -> None:
        super().__init__(name=f"tf[{symbol}:{timeframe}]", bus=bus, parent=parent)
        self.symbol = symbol
        self.timeframe = timeframe
        self.subscriptions = (f"data.updated.{symbol}",)
        self._last_bar_time: pd.Timestamp | None = None

        params = indicator_params or {}
        for ind in indicators:
            IndicatorSubAgent(
                bus=bus,
                parent=self,
                symbol=symbol,
                timeframe=timeframe,
                indicator=ind,
                indicator_params=params.get(ind),
                pivot_left=pivot_left,
                pivot_right=pivot_right,
                include_hidden=include_hidden,
                min_score=min_score,
                max_signal_age_bars=max_signal_age_bars,
            )

    async def handle(self, msg: Message) -> None:
        base_df: pd.DataFrame = msg.payload
        df_tf = resample_ohlcv(base_df, self.timeframe)
        if len(df_tf) < 2:
            return

        # Drop the last (possibly still-forming) bar: closed bars only.
        # Divergence analysis on a repainting bar is how bad systems lie.
        closed = df_tf.iloc[:-1]
        if len(closed) < 60:
            return

        newest = closed.index[-1]
        is_new_bar = self._last_bar_time is None or newest > self._last_bar_time

        # Feed the sub-agents (they scan on every update of the closed frame)
        await self.emit(f"data.tf.{self.symbol}.{self.timeframe}", closed)

        if is_new_bar:
            # Emit every bar that closed since the last update (catch-up safe)
            start = (
                closed.index.searchsorted(self._last_bar_time, side="right")
                if self._last_bar_time is not None
                else len(closed) - 1
            )
            atr_series = atr(closed["high"], closed["low"], closed["close"], 14)
            for ts, row in closed.iloc[start:].iterrows():
                await self.emit(
                    f"bar.closed.{self.symbol}.{self.timeframe}",
                    {
                        "symbol": self.symbol,
                        "timeframe": self.timeframe,
                        "time": ts,
                        "open": float(row["open"]),
                        "high": float(row["high"]),
                        "low": float(row["low"]),
                        "close": float(row["close"]),
                        "atr": float(atr_series.loc[ts]) if ts in atr_series.index else 0.0,
                    },
                )
            self._last_bar_time = newest
