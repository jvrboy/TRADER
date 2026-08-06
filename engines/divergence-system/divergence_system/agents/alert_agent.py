"""
alert_agent.py — AlertAgent: the colony's voice to the outside world.

Subscribes to every meaningful event (births, state changes, deaths,
confluence shifts) and turns them into human-readable alerts. Sinks:

  * console       — always on (can be silenced with verbose=False)
  * JSONL file    — append-only audit log, one JSON event per line
  * webhook       — optional POST of the event JSON (fire-and-forget)

It also republishes every alert on the `alert` topic so any future agent
(Telegram bot, dashboard, trade executor) can subscribe without touching
this code.
"""

from __future__ import annotations

import asyncio
import json
import time

from ..lifecycle import LivingDivergence
from .base import BaseAgent
from .bus import Message, MessageBus


class AlertAgent(BaseAgent):
    subscriptions = ("divergence.*", "confluence.report")

    def __init__(
        self,
        bus: MessageBus,
        alerts_file: str | None = None,
        webhook_url: str = "",
        verbose: bool = True,
        parent: BaseAgent | None = None,
    ) -> None:
        super().__init__(name="alerts", bus=bus, parent=parent)
        self.alerts_file = alerts_file
        self.webhook_url = webhook_url
        self.verbose = verbose
        self.alerts_emitted = 0

    async def handle(self, msg: Message) -> None:
        if msg.topic == "divergence.scored":
            return  # raw signals are lifecycle-agent food, not user alerts

        text, event = self._format(msg)
        if text is None:
            return
        self.alerts_emitted += 1

        if self.verbose:
            print(text)
        if self.alerts_file:
            with open(self.alerts_file, "a", encoding="utf-8") as f:
                f.write(json.dumps(event, default=str) + "\n")
        if self.webhook_url:
            asyncio.get_running_loop().run_in_executor(None, self._post, event)

        await self.emit("alert", event)

    # ------------------------------------------------------------------
    def _format(self, msg: Message) -> tuple[str | None, dict]:
        ts = time.strftime("%H:%M:%S")

        if msg.topic in ("divergence.born", "divergence.state", "divergence.died"):
            ld: LivingDivergence = msg.payload
            d = ld.div
            head = {
                "divergence.born": "BORN",
                "divergence.state": ld.state.upper(),
                "divergence.died": ld.state.upper(),
            }[msg.topic]
            icon = {
                "BORN": "+", "ACTIVE": ">", "HALFWAY": ">>",
                "COMPLETED": "WIN", "INVALIDATED": "STOP", "EXPIRED": "OLD",
            }.get(head, "?")
            text = (
                f"[{ts}] [{icon:>4}] #{ld.id:<4} {d.symbol} {d.timeframe:<4} "
                f"{d.indicator:<10} {d.signal:<16} grade={d.grade:<2} "
                f"score={d.score:5.1f} entry={ld.entry_price:.4f} "
                f"target={ld.target_price:.4f} inval={ld.invalidation_price:.4f} "
                f"bars={ld.bars_alive}"
            )
            return text, {"event": msg.topic, "ts": msg.ts, **ld.to_dict()}

        if msg.topic == "confluence.report":
            r = msg.payload
            text = (
                f"[{ts}] [CONF] {r['symbol']} {r['direction'].upper():<8} "
                f"{r['previous_strength']} -> {r['strength']} "
                f"(score {r['confluence_score']:.1f}, {r['active_count']} live signals, "
                f"TFs: {','.join(r['timeframes']) or '-'}, "
                f"inds: {','.join(r['indicators']) or '-'})"
            )
            return text, {"event": "confluence.report", "ts": msg.ts, **r}

        return None, {}

    # ------------------------------------------------------------------
    def _post(self, event: dict) -> None:
        try:
            import urllib.request

            req = urllib.request.Request(
                self.webhook_url,
                data=json.dumps(event, default=str).encode(),
                headers={"Content-Type": "application/json"},
            )
            urllib.request.urlopen(req, timeout=5)
        except Exception:
            pass  # alerts must never crash the colony
