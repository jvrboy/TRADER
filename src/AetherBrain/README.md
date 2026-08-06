# Aether Brain — Native C# Agentic Research Toolkit

Aether Brain is a pure C#/.NET 8 reference architecture for experimenting with agent orchestration, recurrent neuron graphs, adaptive confidence weighting, layered memory, technical indicators, and forex divergence detection.

## Included systems

- Recurrent neuron graph with bounded activations and adaptive weights
- Working, episodic, and semantic memory with deterministic vector recall
- Five parallel sub-agents: market structure, divergence, risk, memory research, and reflection
- RSI, EMA, ATR, pivot detection, and regular/hidden divergence analysis
- Confidence-weighted consensus and feedback reinforcement
- Optional `HttpClient` adapter for syncing memory to the companion Netlify Database API
- Extensible asynchronous tool registry

## Run

Install the .NET 8 SDK, open a terminal in this folder, and run:

```bash
dotnet run
```

The included program generates deterministic demonstration candles and prints a full multi-agent report. Replace the demo series with broker or CSV data that you have independently validated.

## Safety boundary

This project is research and educational software. It does not place trades, guarantee learning, promise profitability, or provide financial advice. “Learning” means bounded adaptation of confidence weights from explicit feedback—not uncontrolled self-modification.
