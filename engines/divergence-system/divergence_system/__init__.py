"""
Divergence System — a multi-timeframe, multi-indicator, lifecycle-aware
divergence detection engine.

Quick start:

    from divergence_system import (
        MultiTimeframeScanner, AliveDivergenceEngine, SyntheticProvider
    )

    provider = SyntheticProvider()
    scanner = MultiTimeframeScanner(
        timeframes=["5m", "15m", "1h", "4h"],
        indicators=["rsi", "macd", "stochastic", "cci", "mfi", "obv"],
    )
    engine = AliveDivergenceEngine(
        scanner=scanner,
        fetch_fn=lambda s: provider.fetch(s),
        symbols=["BTC/USDT"],
    )
    engine.run_forever()
"""

from .agents import (
    AlertAgent,
    BaseAgent,
    ConfluenceAgent,
    IndicatorSubAgent,
    LifecycleAgent,
    MasterOrchestrator,
    Message,
    MessageBus,
    TimeframeAgent,
)
from .alive import (
    AliveDivergenceEngine,
    ConsoleAlertHandler,
    JsonlAlertHandler,
    TrackedDivergence,
    WebhookAlertHandler,
)
from .config import Config
from .lifecycle import LivingDivergence, ReliabilityTracker
from .data import CCXTProvider, SyntheticProvider, YFinanceProvider, get_provider
from .divergence import Divergence, detect_divergences
from .indicators import AVAILABLE_INDICATORS, compute_indicator
from .mtf import ConfluenceReport, MultiTimeframeScanner, resample_ohlcv
from .pivots import Pivot, find_pivots
from .scoring import grade_for, score_divergence

__version__ = "1.0.0"

__all__ = [
    "AlertAgent",
    "BaseAgent",
    "ConfluenceAgent",
    "IndicatorSubAgent",
    "LifecycleAgent",
    "LivingDivergence",
    "MasterOrchestrator",
    "Message",
    "MessageBus",
    "ReliabilityTracker",
    "TimeframeAgent",
    "AliveDivergenceEngine",
    "ConsoleAlertHandler",
    "JsonlAlertHandler",
    "WebhookAlertHandler",
    "TrackedDivergence",
    "Config",
    "CCXTProvider",
    "SyntheticProvider",
    "YFinanceProvider",
    "get_provider",
    "Divergence",
    "detect_divergences",
    "AVAILABLE_INDICATORS",
    "compute_indicator",
    "ConfluenceReport",
    "MultiTimeframeScanner",
    "resample_ohlcv",
    "Pivot",
    "find_pivots",
    "grade_for",
    "score_divergence",
]
