"""
base.py — BaseAgent: the common skeleton every agent and sub-agent shares.

An agent is an independently running asyncio task with:
  * a unique name and an optional parent (sub-agents have a parent)
  * a set of subscribed topics on the shared MessageBus
  * a `handle(msg)` coroutine invoked for every received message
  * an optional `tick()` coroutine invoked on a fixed interval (heartbeat)
  * lifecycle management: start / stop / crash isolation

Sub-agents are spawned BY agents (e.g. each TimeframeAgent spawns one
IndicatorSubAgent per indicator) and are supervised by their parent:
if a sub-agent crashes, the error is contained and logged — one bad
indicator never takes down the colony.
"""

from __future__ import annotations

import asyncio
import logging
import traceback

from .bus import Message, MessageBus

log = logging.getLogger("divergence.agents")


class BaseAgent:
    #: topics this agent subscribes to; override in subclasses or __init__
    subscriptions: tuple[str, ...] = ()
    #: seconds between tick() calls; 0 disables the heartbeat
    tick_interval: float = 0.0

    def __init__(self, name: str, bus: MessageBus, parent: "BaseAgent | None" = None) -> None:
        self.name = name
        self.bus = bus
        self.parent = parent
        self.children: list[BaseAgent] = []
        self._tasks: list[asyncio.Task] = []
        self._running = False
        self._restarts = 0
        self.max_restarts = 3
        if parent is not None:
            parent.children.append(self)

    # ------------------------------------------------------------------ API
    async def setup(self) -> None:
        """Override: one-time initialization before the loops start."""

    async def handle(self, msg: Message) -> None:
        """Override: react to a message from a subscribed topic."""

    async def tick(self) -> None:
        """Override: periodic heartbeat work."""

    async def teardown(self) -> None:
        """Override: cleanup on stop."""

    async def emit(self, topic: str, payload) -> None:
        await self.bus.publish(topic, payload, sender=self.name)

    # ------------------------------------------------------------- lifecycle
    async def start(self) -> None:
        if self._running:
            return
        self._running = True
        await self.setup()

        for topic in self.subscriptions:
            queue = self.bus.subscribe(topic)
            self._tasks.append(
                asyncio.create_task(self._consume(queue), name=f"{self.name}:consume:{topic}")
            )
        if self.tick_interval > 0:
            self._tasks.append(asyncio.create_task(self._heartbeat(), name=f"{self.name}:tick"))

        for child in self.children:
            await child.start()
        log.debug("agent %s started (%d children)", self.name, len(self.children))

    async def stop(self) -> None:
        self._running = False
        for child in self.children:
            await child.stop()
        for task in self._tasks:
            task.cancel()
        await asyncio.gather(*self._tasks, return_exceptions=True)
        self._tasks.clear()
        await self.teardown()

    # ------------------------------------------------------------- internals
    async def _consume(self, queue: asyncio.Queue) -> None:
        while self._running:
            msg = await queue.get()
            try:
                await self.handle(msg)
            except asyncio.CancelledError:
                raise
            except Exception:
                await self._on_crash("handle", traceback.format_exc())

    async def _heartbeat(self) -> None:
        while self._running:
            try:
                await self.tick()
            except asyncio.CancelledError:
                raise
            except Exception:
                await self._on_crash("tick", traceback.format_exc())
            await asyncio.sleep(self.tick_interval)

    async def _on_crash(self, where: str, tb: str) -> None:
        """Crash isolation: one bad message never takes down the system."""
        self._restarts += 1
        log.error(
            "agent %s crashed in %s (restart %d/%d)\n%s",
            self.name, where, self._restarts, self.max_restarts, tb,
        )
        await self.emit("agent.error", {"agent": self.name, "where": where, "traceback": tb})
        if self._restarts > self.max_restarts:
            log.critical("agent %s exceeded max restarts, stopping", self.name)
            self._running = False

    # ----------------------------------------------------------------- misc
    def tree(self, depth: int = 0) -> str:
        """Render this agent and its sub-agents as an ASCII tree."""
        lines = ["  " * depth + ("└─ " if depth else "") + self.name]
        for child in self.children:
            lines.append(child.tree(depth + 1))
        return "\n".join(lines)
