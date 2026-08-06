"""
bus.py — Async publish/subscribe message bus for inter-agent communication.

Every agent in the system communicates exclusively through this bus.
Agents never call each other directly — they publish typed messages to
topics and subscribe to the topics they care about. This keeps agents
fully decoupled: you can add, remove, or replace agents without touching
any other agent's code.

Topics used by the system:
  data.updated.<symbol>        — new base OHLCV data available for a symbol
  data.tf.<symbol>.<tf>        — resampled closed-bar frame for a timeframe
  bar.closed.<symbol>.<tf>     — a bar closed on a timeframe (lifecycle clock)
  divergence.scored            — an indicator sub-agent found a scored signal
  divergence.born              — lifecycle agent registered a living divergence
  divergence.state             — a living divergence changed state
  divergence.died              — a divergence completed / invalidated / expired
  confluence.report            — confluence agent published a multi-TF report
  alert                        — alert agent emitted a user-facing alert
  agent.error                  — an agent crashed (supervision)
"""

from __future__ import annotations

import asyncio
import itertools
import time
from collections import defaultdict
from dataclasses import dataclass, field
from typing import Any, Callable, Coroutine

_msg_counter = itertools.count(1)


@dataclass
class Message:
    topic: str
    payload: Any
    sender: str
    id: int = field(default_factory=lambda: next(_msg_counter))
    ts: float = field(default_factory=time.time)


Handler = Callable[[Message], Coroutine[Any, Any, None]]


class MessageBus:
    """Asyncio pub/sub bus with per-subscriber queues and a full audit log."""

    def __init__(self, max_log: int = 5000) -> None:
        self._subscribers: dict[str, list[asyncio.Queue]] = defaultdict(list)
        self._log: list[Message] = []
        self._max_log = max_log

    def subscribe(self, topic: str) -> asyncio.Queue:
        """Subscribe to a topic. Supports wildcard prefixes:
        'divergence.*' matches 'divergence.found' etc., '*' matches all."""
        q: asyncio.Queue = asyncio.Queue()
        self._subscribers[topic].append(q)
        return q

    async def publish(self, topic: str, payload: Any, sender: str) -> Message:
        msg = Message(topic=topic, payload=payload, sender=sender)
        self._log.append(msg)
        if len(self._log) > self._max_log:
            self._log = self._log[-self._max_log :]

        # exact-match subscribers
        for q in self._subscribers.get(topic, []):
            q.put_nowait(msg)
        # wildcard subscribers: "divergence.*" matches "divergence.found"
        prefix = topic.split(".")[0] + ".*"
        if prefix != topic:
            for q in self._subscribers.get(prefix, []):
                q.put_nowait(msg)
        # global firehose
        for q in self._subscribers.get("*", []):
            q.put_nowait(msg)
        return msg

    def _all_empty(self) -> bool:
        return all(q.empty() for qs in self._subscribers.values() for q in qs)

    async def drain(self, timeout: float = 10.0) -> None:
        """Wait until every subscriber queue is empty (message cascade settled).
        Used by the replay driver so each injected data step is fully
        processed by the whole colony before the next one is fed in."""
        deadline = time.time() + timeout
        while time.time() < deadline:
            if self._all_empty():
                # yield once more so in-flight handlers can publish follow-ups
                await asyncio.sleep(0)
                if self._all_empty():
                    return
            await asyncio.sleep(0.002)

    def history(self, topic: str | None = None, limit: int = 100) -> list[Message]:
        msgs = self._log if topic is None else [m for m in self._log if m.topic == topic]
        return msgs[-limit:]
