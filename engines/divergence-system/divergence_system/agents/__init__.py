"""
agents — the living, multi-agent layer of the divergence system.

Hierarchy:
    MasterOrchestrator
      ├─ TimeframeAgent (per symbol x timeframe)
      │    └─ IndicatorSubAgent (per indicator)
      ├─ LifecycleAgent   (living divergences, reliability memory)
      ├─ ConfluenceAgent  (multi-TF consensus)
      └─ AlertAgent       (console / JSONL / webhook)

All communication flows through the MessageBus — agents never call each
other directly.
"""

from .alert_agent import AlertAgent
from .base import BaseAgent
from .bus import Message, MessageBus
from .confluence_agent import ConfluenceAgent
from .indicator_agent import IndicatorSubAgent
from .lifecycle_agent import LifecycleAgent
from .orchestrator import MasterOrchestrator
from .timeframe_agent import TimeframeAgent

__all__ = [
    "AlertAgent",
    "BaseAgent",
    "ConfluenceAgent",
    "IndicatorSubAgent",
    "LifecycleAgent",
    "MasterOrchestrator",
    "Message",
    "MessageBus",
    "TimeframeAgent",
]
