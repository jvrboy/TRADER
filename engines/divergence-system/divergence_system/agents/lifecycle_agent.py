"""
lifecycle_agent.py — LifecycleAgent: keeper of the LIVING divergences.

This is what makes the system "alive". A scored divergence is not fired
into the void as a one-off alert — the LifecycleAgent adopts it as a
LivingDivergence and follows it bar by bar until it dies:

  * subscribes to `divergence.scored` — adopts new signals (deduplicated)
  * subscribes to `bar.*` (every `bar.closed.<symbol>.<tf>`) — ages every
    living divergence of that symbol/timeframe with the closed bar
  * publishes `divergence.born` / `divergence.state` / `divergence.died`
  * feeds every death into a ReliabilityTracker, so the colony learns
    which indicator@timeframe combinations actually resolve in its favor
"""

from __future__ import annotations

from ..lifecycle import LivingDivergence, ReliabilityTracker
from .base import BaseAgent
from .bus import Message, MessageBus


class LifecycleAgent(BaseAgent):
    subscriptions = ("divergence.scored", "bar.*")

    def __init__(
        self,
        bus: MessageBus,
        target_atr_multiple: float = 2.0,
        expiry_bars: int = 40,
        parent: BaseAgent | None = None,
    ) -> None:
        super().__init__(name="lifecycle", bus=bus, parent=parent)
        self.target_atr_multiple = target_atr_multiple
        self.expiry_bars = expiry_bars
        self.alive: dict[str, LivingDivergence] = {}
        self.graveyard: list[LivingDivergence] = []
        self.reliability = ReliabilityTracker()

    # ------------------------------------------------------------------
    async def handle(self, msg: Message) -> None:
        if msg.topic == "divergence.scored":
            await self._adopt(msg)
        elif msg.topic.startswith("bar.closed."):
            await self._age(msg)

    # ------------------------------------------------------------------
    async def _adopt(self, msg: Message) -> None:
        d = msg.payload["divergence"]
        atr_val = msg.payload.get("atr", 0.0)
        last_close = msg.payload.get("last_close", d.price_end)

        ld = LivingDivergence(
            div=d,
            entry_price=last_close,
            atr=atr_val,
            target_atr_multiple=self.target_atr_multiple,
            expiry_bars=self.expiry_bars,
        )
        if ld.key in self.alive:
            return  # already tracking this exact divergence
        # also skip if we already resolved this exact signal recently
        if any(g.key == ld.key for g in self.graveyard[-200:]):
            return

        self.alive[ld.key] = ld
        await self.emit("divergence.born", ld)

    # ------------------------------------------------------------------
    async def _age(self, msg: Message) -> None:
        bar = msg.payload
        symbol, tf = bar["symbol"], bar["timeframe"]

        dead_keys: list[str] = []
        for key, ld in self.alive.items():
            if ld.div.symbol != symbol or ld.div.timeframe != tf:
                continue
            change = ld.update(bar["high"], bar["low"], bar["close"])
            if change is None:
                continue
            if ld.alive:
                await self.emit("divergence.state", ld)
            else:
                dead_keys.append(key)
                self.graveyard.append(ld)
                self.reliability.record(ld)
                await self.emit("divergence.died", ld)

        for key in dead_keys:
            del self.alive[key]

    # ------------------------------------------------------------------
    def snapshot(self) -> dict:
        return {
            "alive": [ld.to_dict() for ld in self.alive.values()],
            "graveyard_size": len(self.graveyard),
            "reliability": self.reliability.summary(),
        }
