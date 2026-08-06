# NexusBrain — Self-Learning Agentic AI Brain for Trading (native C#)

A production-ready, self-learning agentic AI brain written in **native C# (.NET 8)**.
It combines a neural-network brain with neurons, a multi-layer memory system, a
colony of specialized trading sub-agents, real-time market data from the **Deriv
public API**, and full **divergence** and **forex** analysis systems.

The brain is trained on the **Volatility Index (VI)** and **Drift Switch Index (DSI)**
— Deriv's synthetic indices — and learns online from realised outcomes via
reinforcement learning.

---

## Features

| Area | What it does |
|------|--------------|
| **Neural Brain** | Multi-layer feed-forward network with backpropagation, momentum, and configurable activations. A bank of trainable "neurons." |
| **Self-Learning** | Online reinforcement learning with reward signals, experience replay, adaptive learning rate (meta-learning), and epsilon-greedy exploration. |
| **Memory System** | Working memory (short-term), episodic memory (event recall with cosine-similarity search), and semantic memory (durable facts). Persists to disk. |
| **Deriv Public API** | Real-time WebSocket client (no API keys) for ticks, OHLC candles, and streaming — Volatility Index, Drift Switch Index, and forex pairs. |
| **Volatility Index** | Normalised VI composite (realised vol + ATR ratio) with regime classification. |
| **Drift Switch Index** | Normalised DSI (ADX trend strength + MACD momentum) for trend/range switching. |
| **Sub-Agent Colony** | 8 specialised agents: momentum, mean-reversion, divergence, volatility, drift-switch, forex, trend, and risk. Weighted consensus voting. |
| **Divergence Analysis** | Regular + hidden divergences across RSI, MACD histogram and StochRSI, with strength scoring. |
| **Forex Analysis** | Pips, classic pivot points, support/resistance, Fibonacci retracements, candlestick patterns, ATR-based stop/target and risk-reward, position sizing. |
| **CLI** | `train`, `analyze`, `live`, `divergence`, `forex`, `memory`, `test` commands. |
| **Test Suite** | 41 automated tests covering every component. |

---

## Project Layout

```
NexusBrain.sln
Directory.Build.props
src/
  NexusBrain.Core/        Data models, series math, config, training-data generator
  NexusBrain.Indicators/  20+ indicators: trend, oscillators, volatility (VI/DSI), smart money, forex, statistics, feature extractor
  NexusBrain.Deriv/       Deriv public WebSocket API client (live data)
  NexusBrain.Brain/       Neural network, self-learning engine, Brain class
  NexusBrain.Memory/      Working, episodic, semantic memory + persistence
  NexusBrain.Divergence/  Divergence detection engine
  NexusBrain.Forex/       Forex analysis system
  NexusBrain.Agents/      Sub-agent colony (8 agents) + consensus
  NexusBrain.Orchestrator/ The "CEO" that coordinates everything
  NexusBrain.Cli/         Command-line interface
tests/
  NexusBrain.Tests/       Automated test suite (41 tests)
data/
  brains/  knowledge/  signals/  models/   (runtime state, gitignored)
docs/
  ARCHITECTURE.md
```

---

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or newer)
- Internet connection for live Deriv data (optional — offline demo works with synthetic data)

## Build

```bash
dotnet build
```

## Run the test suite

```bash
dotnet run --project tests/NexusBrain.Tests
```

## Use the CLI

```bash
cd src/NexusBrain.Cli

# Train the brain on Volatility Index + Drift Switch Index data
dotnet run -- train

# Analyze a symbol (offline demo data)
dotnet run -- analyze R_100

# Analyze with LIVE data from the Deriv public API
dotnet run -- live R_100
dotnet run -- live frxEURUSD
dotnet run -- live 1HZ1000V

# Divergence analysis
dotnet run -- divergence R_100

# Forex analysis
dotnet run -- forex frxEURUSD

# Inspect the brain's memory
dotnet run -- memory

# Built-in self-test
dotnet run -- test
```

---

## How the brain learns

1. **Feature extraction** — every market snapshot is converted into a 24-dimension
   normalised feature vector (RSI, MACD, ADX, Volatility Index, Drift Switch Index,
   ATR, Bollinger %B, order flow, entropy, autocorrelation, candle geometry, ...).
2. **Supervised training** — the neural network learns to map features → forward
   direction from labelled VI/DSI samples.
3. **Reinforcement learning** — after each real outcome, the brain is rewarded
   (positive return → pull prediction up; negative → pull down) and stores the
   episode in memory for replay.
4. **Experience replay** — random mini-batches from the replay buffer further
   consolidate learning.
5. **Meta-learning** — the learning rate adapts: rises while loss improves,
   decays on plateaus.
6. **Persistence** — brain weights, episodic and semantic memory are saved to
   `data/` and reloaded on next start.

The brain keeps learning every time it sees a new outcome — it is never "finished."

---

## Deriv public API

The client connects to `wss://ws.derivws.com/websockets/v3` (app_id 1089, the
public default) — no API key or login required. It can:

- Fetch OHLC candles for any symbol (`ticks_history` + `granularity`)
- Fetch the latest tick / quote
- Stream live ticks (`subscribe`)
- List active symbols

Symbols: Volatility Index (`R_10`…`R_100`, `1HZ10V`…`1HZ100V`), Drift Switch
Index (`1HZ150V`…`1HZ1000V`), forex (`frxEURUSD`, `frxGBPUSD`, `frxUSDJPY`, ...).

---

## Disclaimer

This software is an educational/research tool. It is **not** financial advice and
does **not** guarantee profitable trading. Markets are inherently unpredictable;
always manage risk and test thoroughly before using any signal with real capital.
