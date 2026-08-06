"""
orchestrator.py — MasterOrchestrator: spawns and supervises the whole colony.

Agent hierarchy it builds from a Config:

    orchestrator
      ├─ tf[SYMBOL:TF]            (one per symbol x timeframe)
      │    ├─ ind[rsi]@SYMBOL:TF  (one sub-agent per indicator)
      │    ├─ ind[macd]@SYMBOL:TF
      │    └─ ...
      ├─ lifecycle                (living divergences + reliability stats)
      ├─ confluence               (multi-TF consensus)
      └─ alerts                   (console / JSONL / webhook)

Two ways to drive it:

  * run_replay(base_df, warmup, step) — feeds historical data through the
    colony step by step, fully deterministic, great for research/backtests.
  * run_live(fetch_fn, poll_seconds)  — polls a data provider forever and
    feeds each snapshot to the colony. Ctrl-C to stop.
"""

from __future__ import annotations

import asyncio

import pandas as pd

from ..config import Config
from .alert_agent import AlertAgent
from .base import BaseAgent
from .bus import MessageBus
from .confluence_agent import ConfluenceAgent
from .lifecycle_agent import LifecycleAgent
from .timeframe_agent import TimeframeAgent


class MasterOrchestrator(BaseAgent):
    def __init__(self, cfg: Config, provider=None) -> None:
        bus = MessageBus()
        super().__init__(name="orchestrator", bus=bus)
        self.cfg = cfg
        self.provider = provider

        for symbol in cfg.symbols:
            for tf in cfg.timeframes:
                TimeframeAgent(
                    bus=bus,
                    symbol=symbol,
                    timeframe=tf,
                    indicators=cfg.indicators,
                    indicator_params=cfg.indicator_params,
                    pivot_left=cfg.pivot_left,
                    pivot_right=cfg.pivot_right,
                    include_hidden=cfg.include_hidden,
                    min_score=cfg.min_score,
                    max_signal_age_bars=cfg.max_signal_age_bars,
                    parent=self,
                )

        self.lifecycle = LifecycleAgent(
            bus=bus,
            target_atr_multiple=cfg.target_atr_multiple,
            expiry_bars=cfg.expiry_bars,
            parent=self,
        )
        self.confluence = ConfluenceAgent(
            bus=bus,
            timeframes=cfg.timeframes,
            indicators=cfg.indicators,
            parent=self,
        )
        self.alerts = AlertAgent(
            bus=bus,
            alerts_file=cfg.alerts_file,
            webhook_url=cfg.webhook_url,
            parent=self,
        )

    # ------------------------------------------------------------------
    async def feed(self, symbol: str, base_df: pd.DataFrame) -> None:
        """Inject a base-timeframe OHLCV snapshot and wait for the colony
        to finish reacting to it."""
        await self.emit(f"data.updated.{symbol}", base_df)
        await self.bus.drain()

    # ------------------------------------------------------------------
    async def run_replay(
        self, base_df: pd.DataFrame, warmup: int = 1000, step: int = 12,
        symbol: str | None = None,
    ) -> dict:
        """Replay history through the live colony. `warmup` bars are needed
        before the first scan; then the frame grows by `step` base bars per
        iteration (simulating time passing)."""
        symbol = symbol or self.cfg.symbols[0]
        await self.start()
        try:
            for end in range(warmup, len(base_df) + 1, step):
                await self.feed(symbol, base_df.iloc[:end])
        finally:
            await self.stop()
        return self.summary()

    # ------------------------------------------------------------------
    async def run_live(self, fetch_fn, poll_seconds: float = 60.0) -> None:
        """Poll `fetch_fn(symbol) -> DataFrame` forever, feeding the colony."""
        await self.start()
        print(self.tree())
        print(f"\ncolony alive — polling every {poll_seconds:.0f}s. Ctrl-C to stop.\n")
        try:
            while True:
                for symbol in self.cfg.symbols:
                    try:
                        base_df = await asyncio.get_running_loop().run_in_executor(
                            None, fetch_fn, symbol
                        )
                        await self.feed(symbol, base_df)
                    except Exception as exc:
                        print(f"[data] fetch failed for {symbol}: {exc}")
                await asyncio.sleep(poll_seconds)
        except (KeyboardInterrupt, asyncio.CancelledError):
            pass
        finally:
            await self.stop()

    # ------------------------------------------------------------------
    def summary(self) -> dict:
        snap = self.lifecycle.snapshot()
        return {
            "alerts_emitted": self.alerts.alerts_emitted,
            "graveyard_size": snap["graveyard_size"],
            "alive": snap["alive"],
            "reliability": snap["reliability"],
        }
