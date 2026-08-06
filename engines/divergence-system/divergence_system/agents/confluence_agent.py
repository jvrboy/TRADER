"""
confluence_agent.py — ConfluenceAgent: the colony's big-picture analyst.

Individual agents see one indicator on one timeframe. The ConfluenceAgent
watches every birth and death of living divergences across ALL timeframes
and indicators and continuously re-derives the multi-timeframe consensus
per symbol and direction. When the consensus strength changes (e.g. a 4h
RSI divergence joins an existing 1h MACD one and the picture jumps from
MODERATE to STRONG), it publishes a `confluence.report`.
"""

from __future__ import annotations

from ..lifecycle import LivingDivergence
from ..mtf import TIMEFRAME_MINUTES
from .base import BaseAgent
from .bus import Message, MessageBus

STRENGTHS = ["NONE", "WEAK", "MODERATE", "STRONG", "EXTREME"]


class ConfluenceAgent(BaseAgent):
    subscriptions = ("divergence.*",)

    def __init__(
        self,
        bus: MessageBus,
        timeframes: list[str],
        indicators: list[str],
        parent: BaseAgent | None = None,
    ) -> None:
        super().__init__(name="confluence", bus=bus, parent=parent)
        self.timeframes = timeframes
        self.indicators = indicators
        self.active: dict[str, LivingDivergence] = {}
        # last published strength per (symbol, direction)
        self._last_strength: dict[tuple[str, str], str] = {}

    async def handle(self, msg: Message) -> None:
        if msg.topic == "divergence.born":
            ld: LivingDivergence = msg.payload
            self.active[ld.key] = ld
            await self._reassess(ld.div.symbol, ld.div.direction)
        elif msg.topic == "divergence.died":
            ld = msg.payload
            self.active.pop(ld.key, None)
            await self._reassess(ld.div.symbol, ld.div.direction)

    # ------------------------------------------------------------------
    async def _reassess(self, symbol: str, direction: str) -> None:
        group = [
            ld for ld in self.active.values()
            if ld.div.symbol == symbol and ld.div.direction == direction
        ]
        score, strength, tfs, inds = self._confluence(group)

        key = (symbol, direction)
        previous = self._last_strength.get(key, "NONE")
        if strength == previous:
            return
        self._last_strength[key] = strength

        await self.emit(
            "confluence.report",
            {
                "symbol": symbol,
                "direction": direction,
                "confluence_score": round(score, 2),
                "strength": strength,
                "previous_strength": previous,
                "timeframes": tfs,
                "indicators": inds,
                "active_count": len(group),
                "signals": [ld.to_dict() for ld in group],
            },
        )

    # ------------------------------------------------------------------
    def _confluence(
        self, group: list[LivingDivergence]
    ) -> tuple[float, str, list[str], list[str]]:
        if not group:
            return 0.0, "NONE", [], []

        tfs = sorted(
            {ld.div.timeframe for ld in group},
            key=lambda t: TIMEFRAME_MINUTES.get(t, 0),
        )
        inds = sorted({ld.div.indicator for ld in group})
        avg_quality = sum(ld.div.score for ld in group) / len(group)
        tf_weight = min(len(tfs) / max(len(self.timeframes), 1), 1.0)
        ind_weight = min(len(inds) / max(len(self.indicators), 1), 1.0)
        htf_bonus = 10.0 if self.timeframes and self.timeframes[-1] in tfs else 0.0

        score = min(
            100.0, avg_quality * 0.5 + tf_weight * 25.0 + ind_weight * 15.0 + htf_bonus
        )
        strength = (
            "EXTREME" if score >= 80 else
            "STRONG" if score >= 65 else
            "MODERATE" if score >= 45 else
            "WEAK"
        )
        return score, strength, tfs, inds
