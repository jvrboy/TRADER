# Divergence System

A production-grade, multi-timeframe, multi-indicator divergence detection
engine in pure Python. It doesn't check one timeframe — it checks them all,
thoroughly, and it treats every divergence as a **living object** with a full
lifecycle.

## Features

- **11 oscillators**: RSI, MACD, MACD histogram, Stochastic, CCI, MFI, OBV,
  Momentum, ROC, Williams %R, Awesome Oscillator — all pure pandas/numpy,
  no TA-Lib needed.
- **All 4 divergence types**: regular bullish/bearish (reversal) and hidden
  bullish/bearish (continuation).
- **Non-repainting pivots**: fractal swing detection with explicit
  confirmation lag — signals only fire once fully confirmed.
- **Pivot alignment tolerance**: price and oscillator pivots rarely land on
  the same bar; the engine aligns them intelligently instead of missing them.
- **Line-cut validation**: rejects "divergences" whose connecting line is
  broken by the data in between.
- **Quality scoring (0–100) + grades (A+/A/B/C/D)** based on slope
  disagreement, oscillator zone, freshness, pivot span, trend context, and
  volume confirmation.
- **Multi-timeframe confluence**: scans 1m→1w, resampled from a single base
  feed, and reports how many independent timeframe × indicator pairs agree
  (WEAK / MODERATE / STRONG / EXTREME).
- **ALIVE engine**: continuous scanning loop with a full signal lifecycle —
  `NEW_SIGNAL → CONFIRMED → PLAYING_OUT → COMPLETED / INVALIDATED / EXPIRED`
  — with deduplication across cycles, ATR-based targets and invalidation
  levels, and pluggable alerts (console, JSON-lines file, webhook).
- **Multi-agent colony**: a supervised hierarchy of asyncio agents and
  sub-agents — one TimeframeAgent per symbol × timeframe, each spawning one
  IndicatorSubAgent per oscillator, plus LifecycleAgent (living divergences +
  per-indicator reliability memory), ConfluenceAgent (cross-TF consensus
  shifts), and AlertAgent (console / JSONL / webhook). All communication
  flows through a pub/sub MessageBus; a crashing agent is isolated and never
  takes down the colony.
- **Replay backtesting**: drive the entire colony deterministically over
  historical data (`main.py replay`) and get objective win-rate statistics
  per indicator@timeframe.
- **3 data providers**: yfinance (stocks/FX/crypto), ccxt (any crypto
  exchange), and a synthetic generator so everything runs offline.
- **Charting**: dark-theme matplotlib plots with divergence lines drawn on
  both price and oscillator panels.

## Install

```bash
pip install -r requirements.txt
```

Only `numpy` and `pandas` are strictly required. Install `yfinance`/`ccxt`
for live data and `matplotlib` for charts.

## Quick start

```bash
# Offline demo scan (synthetic data — works with zero setup)
python main.py scan

# Real crypto, one-shot multi-timeframe scan
python main.py scan --provider ccxt --symbols BTC/USDT ETH/USDT

# Stocks via Yahoo Finance
python main.py scan --provider yfinance --symbols AAPL TSLA --interval 5m

# The ALIVE engine — continuous scanning, lifecycle tracking, alerts
python main.py live --provider ccxt --symbols BTC/USDT --poll 60

# The AGENT COLONY — live multi-agent system (timeframe agents,
# indicator sub-agents, lifecycle, confluence, alerts)
python main.py colony --provider ccxt --symbols BTC/USDT --poll 60

# Replay history through the colony and get reliability stats
python main.py replay --provider ccxt --symbols BTC/USDT --warmup 1000 --step 12

# JSON output / chart output
python main.py scan --json
python main.py scan --plot chart.png --plot-indicator rsi

# Everything from a config file
python main.py live --config config.example.json
```

## Library usage

```python
from divergence_system import (
    MultiTimeframeScanner, AliveDivergenceEngine, get_provider
)

provider = get_provider("ccxt", exchange="binance")
scanner = MultiTimeframeScanner(
    timeframes=["5m", "15m", "1h", "4h"],
    indicators=["rsi", "macd", "stochastic", "cci", "mfi", "obv"],
    min_score=50,
)

# One-shot
df = provider.fetch("BTC/USDT", interval="5m", limit=1500)
divs = scanner.scan(df, "BTC/USDT")
for report in scanner.confluence(divs, "BTC/USDT"):
    print(report.to_dict())

# Alive
engine = AliveDivergenceEngine(
    scanner=scanner,
    fetch_fn=lambda s: provider.fetch(s, interval="5m", limit=1500),
    symbols=["BTC/USDT", "ETH/USDT"],
    poll_seconds=60,
)
engine.run_forever()
```

## Architecture

```
divergence_system/
├── indicators.py   # 11 oscillators, pure pandas/numpy
├── pivots.py       # non-repainting fractal pivot detection
├── divergence.py   # 4-type divergence engine with line-cut validation
├── scoring.py      # 0-100 quality score + A+/A/B/C/D grades
├── mtf.py          # multi-timeframe resampling, scanning, confluence
├── lifecycle.py    # LivingDivergence state machine + ReliabilityTracker
├── alive.py        # classic single-loop alive engine: lifecycle, dedup, alerts
├── data.py         # yfinance / ccxt / synthetic providers
├── plotting.py     # dark-theme divergence charts
├── config.py       # JSON-overridable configuration
└── agents/         # the multi-agent colony
    ├── bus.py               # pub/sub MessageBus (all inter-agent comms)
    ├── base.py              # BaseAgent: supervision, crash isolation, sub-agents
    ├── timeframe_agent.py   # one agent per symbol x timeframe (closed bars only)
    ├── indicator_agent.py   # one sub-agent per indicator per timeframe
    ├── lifecycle_agent.py   # adopts signals as LivingDivergences, tracks deaths
    ├── confluence_agent.py  # publishes multi-TF consensus strength shifts
    ├── alert_agent.py       # console / JSONL / webhook alert sinks
    └── orchestrator.py      # MasterOrchestrator: builds + drives the colony
main.py             # CLI: scan | live | colony | replay
demo.py             # end-to-end offline demonstration (scan + colony replay)
config.example.json # full config template
```

## Agent hierarchy

```
orchestrator
  ├─ tf[BTC/USDT:5m]
  │    ├─ ind[rsi]@BTC/USDT:5m
  │    ├─ ind[macd]@BTC/USDT:5m
  │    └─ ... one sub-agent per indicator
  ├─ tf[BTC/USDT:15m] ...
  ├─ tf[BTC/USDT:1h]  ...
  ├─ tf[BTC/USDT:4h]  ...
  ├─ lifecycle    # living divergences: born -> active -> halfway -> dead
  ├─ confluence   # cross-TF consensus: WEAK / MODERATE / STRONG / EXTREME
  └─ alerts       # console + JSONL + webhook
```

Message topics: `data.updated.*`, `data.tf.*`, `bar.closed.*`,
`divergence.scored/born/state/died`, `confluence.report`, `alert`,
`agent.error`.

### Colony usage as a library

```python
import asyncio
from divergence_system import Config, MasterOrchestrator, SyntheticProvider

cfg = Config(symbols=["BTC/DEMO"], timeframes=["15m", "1h", "4h"],
             indicators=["rsi", "macd", "stochastic", "cci"])
provider = SyntheticProvider()
base = provider.fetch("BTC/DEMO", interval="5m", bars=3000)

orch = MasterOrchestrator(cfg)
summary = asyncio.run(orch.run_replay(base, warmup=1500, step=12))
print(summary["reliability"])   # win rates per indicator@timeframe
```

## Signal lifecycle

| State | Meaning |
|---|---|
| `NEW_SIGNAL` | Divergence detected and fully confirmed (non-repainting) |
| `CONFIRMED` | Being tracked; target + invalidation levels computed from ATR |
| `PLAYING_OUT` | Price has moved ≥55% of the way toward the target |
| `COMPLETED` | ATR-multiple target reached |
| `INVALIDATED` | Price broke through the divergence pivot — signal failed |
| `EXPIRED` | Aged out without resolving |

## Disclaimer

This software is for research and education. It is not financial advice.
Trading involves substantial risk of loss.
