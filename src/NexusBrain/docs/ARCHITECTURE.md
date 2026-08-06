# NexusBrain Architecture

## Overview

NexusBrain is a self-learning agentic AI brain for trading, built as a layered
C# solution. Data flows from the Deriv public API (or synthetic generator)
through an indicator/feature pipeline into a neural brain, which is trained by
a self-learning engine. A colony of specialized sub-agents independently
analyze the same data and vote on a consensus. Everything is remembered in a
multi-tier memory system and persisted to disk.

```
                 ┌─────────────────────────────────────────────┐
                 │              BrainOrchestrator              │
                 │              (the "CEO")                    │
                 └─────────────────────────────────────────────┘
        ┌──────────────┬───────────────┬──────────────┬──────────────┐
        ▼              ▼               ▼              ▼              ▼
   ┌─────────┐   ┌────────────┐  ┌───────────┐  ┌────────────┐ ┌──────────┐
   │  Brain   │   │ AgentColony │  │  MemorySystem │  │ DerivClient │ │ Forex /  │
   │ (neural  │   │ (8 sub-    │  │ (working +   │  │ (live data) │ │ Divergence│
   │ network) │   │  agents)   │  │ episodic +   │  │             │ │ engines  │
   └─────────┘   └────────────┘  │ semantic)    │  └────────────┘ └──────────┘
        ▲              ▲         └────────────┘
        │              │
        └──────┬───────┘
               ▼
        ┌──────────────┐
        │ FeatureExtractor│  ← 24-dim vector from candles
        └──────────────┘
               ▲
        ┌──────────────┐
        │  Indicators   │  ← RSI, MACD, ADX, VI, DSI, ATR, ...
        └──────────────┘
               ▲
        ┌──────────────┐
        │  Candle data  │  ← Deriv API or synthetic generator
        └──────────────┘
```

## Projects & responsibilities

| Project | Responsibility |
|---------|----------------|
| `NexusBrain.Core` | Domain models (`Candle`, `Signal`, `Bias`, `BrainAnalysis`, `BrainConfig`), `SeriesMath`, and the synthetic `TrainingDataGenerator`. No dependencies. |
| `NexusBrain.Indicators` | Trend, oscillators, volatility (**VI** & **DSI**), smart money, stats, forex helpers, and the `FeatureExtractor`. Depends on Core. |
| `NexusBrain.Deriv` | `DerivClient` — WebSocket client for the Deriv public API. Depends on Core. |
| `NexusBrain.Brain` | `NeuralNetwork` (backprop + momentum), `SelfLearningEngine` (RL + replay + meta-learning), `Brain` (training/prediction/persistence). Depends on Core + Indicators. |
| `NexusBrain.Memory` | `MemorySystem` — working, episodic (similarity recall), semantic memory with JSON persistence. Depends on Core. |
| `NexusBrain.Divergence` | `DivergenceEngine` — regular/hidden divergence detection on RSI/MACD/StochRSI. Depends on Core + Indicators. |
| `NexusBrain.Forex` | `ForexAnalyzer` — pips, pivots, S/R, Fibonacci, patterns, position sizing. Depends on Core + Indicators. |
| `NexusBrain.Agents` | The 8 sub-agents + `AgentColony` with weighted consensus. Depends on Core, Indicators, Brain, Divergence, Forex, Memory, Deriv. |
| `NexusBrain.Orchestrator` | `BrainOrchestrator` coordinating brain + agents + memory + data into a self-learning loop. |
| `NexusBrain.Cli` | Console entry point (`train`, `analyze`, `live`, `divergence`, `forex`, `memory`, `test`). |
| `NexusBrain.Tests` | 41 automated tests across all components. |

## The neural brain

`NeuralNetwork` is a fully-connected MLP with configurable hidden layers and
activations (sigmoid/tanh/ReLU/leaky-ReLU/swish). Training uses backpropagation
with momentum. `SelfLearningEngine` wraps it with:

- **Reinforce(state, target, reward)** — reward-weighted target nudging.
- **Record / Replay(batch)** — experience replay from a buffer.
- **Meta-learning** — adaptive learning rate (rises on improvement, decays on plateau).
- **EpsilonGreedy** — exploration during online learning.

The `Brain` class owns a network + learner, exposes `Predict` (returns a
directional score in [-1, 1]), `TrainEpoch`, `TrainReinforced`, and JSON
save/load of weights + metadata.

## The sub-agent colony

Eight agents each emit `Signal`s (bias, confidence, strength, reasoning):

1. **momentum** — ADX/MACD/ROC trend riding.
2. **mean_reversion** — fade RSI/Stochastic extremes (only when no strong trend).
3. **divergence** — trade regular/hidden divergences.
4. **volatility** — adapt posture to the Volatility Index regime.
5. **drift_switch** — switch between trend and range via the DSI.
6. **forex** — full forex analysis (pivots, S/R, patterns, RR).
7. **trend** — MA alignment + SuperTrend confirmation.
8. **risk** — ATR/VI risk warnings (highest vote weight).

`AgentColony.Aggregate` produces a weighted consensus bias + confidence.

## Memory

- **Working memory** — KV slots with LRU eviction (current context).
- **Episodic memory** — timestamped events with feature signatures; recall by
  event/symbol or by cosine-similarity to a query vector. Outcomes recorded for
  reinforcement.
- **Semantic memory** — durable facts with a reinforcement strength.

Persisted as JSON in `data/knowledge/`.

## Data flow for live analysis

1. `BrainOrchestrator.AnalyzeLiveAsync(symbol, kind, granularity, count)`
2. `DerivClient.GetCandlesAsync(...)` → real OHLC candles from Deriv.
3. `FeatureExtractor.Extract(...)` → 24-dim vector.
4. `Brain.Predict(...)` → directional score.
5. `AgentColony.RunAll(...)` → signals; `Aggregate(...)` → consensus.
6. `DivergenceEngine.Scan(...)` → divergence report.
7. `MemorySystem` records the snapshot in working memory.
8. Returns a `BrainRunResult` with everything.

## Self-learning loop

After a position closes, call
`BrainOrchestrator.LearnFromOutcome(candles, symbol, kind, realisedReturn)`:

- Computes the target from the realised return.
- Reinforces the brain (reward = sign/magnitude of the return).
- Stores an episodic memory (PROFIT/LOSS) with the feature signature.
- Updates a semantic fact about the symbol's recent behaviour.
- Saves brain + memory to disk.

The brain improves continuously with every outcome it observes.
