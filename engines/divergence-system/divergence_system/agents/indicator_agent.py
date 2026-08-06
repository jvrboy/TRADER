"""
indicator_agent.py — IndicatorSubAgent: one oscillator, one timeframe, one job.

Each TimeframeAgent spawns one IndicatorSubAgent per configured indicator
(RSI, MACD, Stochastic, CCI, MFI, OBV, ...). The sub-agent listens for the
closed-bar frame of ITS timeframe, computes ITS oscillator, runs the core
divergence detector, scores every hit, filters by quality, and publishes
`divergence.scored` messages for the LifecycleAgent to adopt.

Crash isolation from BaseAgent means one broken indicator never affects
the rest of the colony.
"""

from __future__ import annotations

import pandas as pd

from ..divergence import detect_divergences
from ..indicators import atr, compute_indicator
from ..scoring import score_divergence
from .base import BaseAgent
from .bus import Message, MessageBus


class IndicatorSubAgent(BaseAgent):
    def __init__(
        self,
        bus: MessageBus,
        parent: BaseAgent,
        symbol: str,
        timeframe: str,
        indicator: str,
        indicator_params: dict | None = None,
        pivot_left: int = 5,
        pivot_right: int = 5,
        include_hidden: bool = True,
        min_score: float = 45.0,
        max_signal_age_bars: int = 12,
    ) -> None:
        name = f"ind[{indicator}]@{symbol}:{timeframe}"
        super().__init__(name=name, bus=bus, parent=parent)
        self.symbol = symbol
        self.timeframe = timeframe
        self.indicator = indicator
        self.indicator_params = indicator_params
        self.pivot_left = pivot_left
        self.pivot_right = pivot_right
        self.include_hidden = include_hidden
        self.min_score = min_score
        self.max_signal_age_bars = max_signal_age_bars
        self.subscriptions = (f"data.tf.{symbol}.{timeframe}",)
        self.signals_found = 0

    async def handle(self, msg: Message) -> None:
        df: pd.DataFrame = msg.payload
        if len(df) < 60:
            return

        osc = compute_indicator(df, self.indicator, self.indicator_params)
        divergences = detect_divergences(
            df,
            osc,
            indicator_name=self.indicator,
            symbol=self.symbol,
            timeframe=self.timeframe,
            pivot_left=self.pivot_left,
            pivot_right=self.pivot_right,
            include_hidden=self.include_hidden,
        )
        if not divergences:
            return

        atr_series = atr(df["high"], df["low"], df["close"], 14)
        current_atr = float(atr_series.iloc[-1]) if len(atr_series) else 0.0
        last_close = float(df["close"].iloc[-1])

        for d in divergences:
            score_divergence(d, df, osc)
            age = len(df) - 1 - d.confirmed_index
            if d.score < self.min_score or age > self.max_signal_age_bars:
                continue
            self.signals_found += 1
            await self.emit(
                "divergence.scored",
                {
                    "divergence": d,
                    "atr": current_atr,
                    "last_close": last_close,
                    "bar_count": len(df),
                },
            )
