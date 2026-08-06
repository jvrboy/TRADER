# DsiAgentic — Native C# Agentic Analysis System

Production-ready **.NET 8** multi-project solution that ports and expands the Python DSI hub into a fully native C# agentic system. Every indicator, agent, sub-brain, micro-kernel, strategy, tracker, and learner is C# — no Python or ONNX runtime required.

## Instruments (7)

| Name    | Deriv Symbol | Family |
|---------|--------------|--------|
| DSI10   | `DSI10`      | drift  |
| DSI20   | `DSI20`      | drift  |
| DSI30   | `DSI30`      | drift  |
| XAUUSD  | `frxXAUUSD`  | fx     |
| AUDCAD  | `frxAUDCAD`  | fx     |
| USDCHF  | `frxUSDCHF`  | fx     |
| USDJPY  | `frxUSDJPY`  | fx     |

## Solution layout

```
DsiAgentic.sln
├── src/DsiAgentic.Core            models, series, config
├── src/DsiAgentic.Deriv           WebSocket client (ticks_history, ticks, ping)
├── src/DsiAgentic.Indicators      20+ oscillators, 15+ trend, volatility, SMC, statistical
├── src/DsiAgentic.Divergence      4 divergence types × 14 oscillators, 0-100 scoring
├── src/DsiAgentic.Strategies      14 strategies (trend, momentum, volatility, SMC, structure)
├── src/DsiAgentic.Kernels         MicroKernel + KernelBank + Neuron
├── src/DsiAgentic.Brains          Per-instrument 6-sub-brain ensemble
├── src/DsiAgentic.Learning        MetaLearner + KnowledgeStore
├── src/DsiAgentic.Agents          9 agents composing 40+ voters
├── src/DsiAgentic.Risk            ATR TP/SL, Kelly, Monte Carlo win-prob
├── src/DsiAgentic.Signals         SignalEngine + SignalTracker
├── src/DsiAgentic.Persistence     SignalStore + PerformanceStore
├── src/DsiAgentic.Orchestrator    MasterOrchestrator + MessageBus
└── src/DsiAgentic.Cli             `dsi` command-line runner
```

## Neural architecture

Each instrument owns a **BrainEnsemble** of six specialised sub-brains:

| Sub-brain    | Head             | Kernel bank                                     |
|--------------|------------------|-------------------------------------------------|
| Outcome      | P(TP)            | RBF, Sigmoid, Swish, Softplus                   |
| Trend        | continuation     | Tanh, Linear, Swish                             |
| Reversal     | mean-reversion   | RBF, Gauss, Tanh                                |
| Volatility   | expansion        | ReLU, Softplus, Sigmoid                         |
| Regime       | regime change    | RBF, Swish, Tanh                                |
| Meta         | ensemble blend   | Sigmoid, Tanh, Swish, Softplus                  |

Every sub-brain is a `Neuron` wrapping a `KernelBank` of `MicroKernel`s (8 activation types: RBF, Sigmoid, Tanh, ReLU, Linear, Softplus, Swish, Gauss). Kernels self-tune via a gradient-free nudge rule ideal for streaming market data. The **Meta** brain takes the five head outputs as its 5-dim input and produces the final win-probability blend.

## Agent colony (9 agents, 40+ voters)

`TrendAgent`, `MomentumAgent`, `VolatilityAgent`, `StructureAgent`, `SmcAgent`, `DivergenceAgent`, `RegimeAgent`, `StatisticalAgent`, `CandleAgent` — each runs its own strategy stack over every timeframe and emits weighted votes. The `AgentColony` aggregates by direction, family, and net confluence score.

## Signal lifecycle (TP or SL only)

1. `MasterOrchestrator.ScanAllAsync` pulls MTF candles for each instrument.
2. `AgentColony.Aggregate` sums weighted votes; `SignalEngine.Generate` applies `MetaLearner` multipliers and thresholds against `min_confluence`.
3. TP / SL computed from ATR × configured multiples; `Monte Carlo` refines win probability; brain ensemble predicts win probability; the two are blended 50/50.
4. `SignalTracker.Update` polls quotes and closes **only** on TP or SL hit — never on time.
5. On close, `MetaLearner` records per-agent / per-family win-rate, brain ensemble learns from the outcome, `KnowledgeStore` appends a `.jsonl` row, `PerformanceStore` updates totals and net pips.

## Build & run

```bash
dotnet build DsiAgentic.sln -c Release
cd src/DsiAgentic.Cli
dotnet run --project . -- ping
dotnet run --project . -- price XAUUSD
dotnet run --project . -- scan
dotnet run --project . -- signal DSI20
dotnet run --project . -- agents DSI30
dotnet run --project . -- divergence XAUUSD
dotnet run --project . -- brain DSI10
dotnet run --project . -- poll
dotnet run --project . -- watch 30000
```

`watch` is the always-on production loop — it repeats `scan` + `poll` on a fixed interval, keeping open signals live, closing on TP/SL, updating brains and meta-learner, and persisting to `data/`.

## Data directory

```
data/
├── open_signals.json           active signals
├── signals.jsonl               closed signals history
├── performance.json            per-instrument win/loss/pip totals
├── meta_learner.json           agent/family win-rate weights
├── brains/
│   ├── DSI10.outcome.json      brain weights per sub-brain
│   ├── DSI10.trend.json
│   ├── DSI10.reversal.json
│   ├── DSI10.volatility.json
│   ├── DSI10.regime.json
│   ├── DSI10.meta.json
│   ├── DSI10.stats.json
│   └── … (× 7 instruments)
└── knowledge/
    ├── DSI10.jsonl              raw learning corpus per instrument
    └── …
```

Every closed trade is a permanent record — brains can be replayed from `data/knowledge/*.jsonl` at any time.

## Requirements

* .NET 8 SDK — nothing else. No external NuGet packages.
* Internet access to `wss://ws.derivws.com/websockets/v3?app_id=1089`.

## Configuration — `config/instruments.json`

Instrument list, timeframes (seconds), min-confluence, ATR TP/SL multiples, runtime rules (`expiry_rule: TP_OR_SL_ONLY`, `one_per_instrument`, `poll_interval_ms`). Editable without recompiling.
