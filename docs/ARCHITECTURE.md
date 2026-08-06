# TRADER Architecture Guide

## Monorepo Layout

```
TRADER/
├── TRADER.sln                       # Master Visual Studio / Rider solution
├── README.md                        # Project documentation & quick start
├── .gitignore                       # Multi-stack gitignore
│
├── src/                             # .NET C# Source Code
│   ├── TraderApp/                   # .NET MAUI cross-platform UI application
│   │   ├── Models/                  # UI and trading data models
│   │   ├── ViewModels/              # MVVM ViewModels for tabs & detail views
│   │   ├── Views/                   # XAML pages (AI Chat, Quotes, Chart, Signals, Bot, Settings)
│   │   ├── Services/                # Application services (AI, Market Data, Indicators, Trading)
│   │   ├── Converters/              # XAML binding converters
│   │   └── Resources/               # Fonts, styles, colors, app icons, splash screens
│   │
│   ├── Trader.Backend/              # Lightweight agentic backend service
│   │   ├── Agents/                  # Swarm specialist agents & SwarmCoordinator
│   │   ├── Core/                    # Tool framework, tool registry, agent runtime
│   │   └── Tools/                   # 22+ market analysis & risk management tools
│   │
│   ├── BrainSystem/                 # Deep learning & cognitive neural engine
│   │   ├── Native/                  # .NET 8 native GGUF runner, tensors, knowledge graph
│   │   ├── Api/                     # ASP.NET Core API server & domain models
│   │   └── Modular/                 # Modular cognitive subsystem (Core, Memory, LLM, Tools, Training, API)
│   │
│   ├── DsiAgentic/                  # Multi-layer agentic trading platform (14 layers)
│   │   ├── src/DsiAgentic.Core/     # Core domain abstractions and series
│   │   ├── src/DsiAgentic.Deriv/    # Deriv WebSocket client
│   │   ├── src/DsiAgentic.Indicators/# 60+ technical indicator calculations
│   │   ├── src/DsiAgentic.Divergence/# RSI & MACD divergence detection engine
│   │   ├── src/DsiAgentic.Strategies/# Algorithmic trading strategies
│   │   ├── src/DsiAgentic.Kernels/  # High-throughput computation kernels
│   │   ├── src/DsiAgentic.Brains/   # Neural ensembles and feature extractors
│   │   ├── src/DsiAgentic.Learning/ # Meta-learning & persistent knowledge store
│   │   ├── src/DsiAgentic.Agents/   # Autonomous agent colony runtime
│   │   ├── src/DsiAgentic.Risk/     # Kelly-criterion & risk manager
│   │   ├── src/DsiAgentic.Signals/  # Signal generator & confidence scorer
│   │   ├── src/DsiAgentic.Persistence/# SQLite / JSON state persistence
│   │   ├── src/DsiAgentic.Orchestrator/# Message bus & task orchestrator
│   │   └── src/DsiAgentic.Cli/      # Console management CLI
│   │
│   ├── NexusBrain/                  # Self-learning neural agentic brain engine
│   │   ├── src/NexusBrain.Core/     # Signal definitions & training data generation
│   │   ├── src/NexusBrain.Indicators/# Oscillators, trend, volatility, smart money
│   │   ├── src/NexusBrain.Brain/    # Feed-forward neural network & self-learning
│   │   ├── src/NexusBrain.Memory/   # Working, episodic & semantic memory
│   │   ├── src/NexusBrain.Forex/    # Multi-pair forex regime analyzer
│   │   ├── src/NexusBrain.Agents/   # Specialized sub-agents & colony
│   │   ├── src/NexusBrain.Orchestrator/# Execution pipeline orchestrator
│   │   └── src/NexusBrain.Cli/      # CLI evaluation harness
│   │
│   ├── AiBrain/                     # Multi-memory cognitive brain (Backprop, Hebbian, RL)
│   └── AetherBrain/                 # Adaptive weight orchestration & market models
│
├── tests/                           # Unit & Integration Test Suites
│   ├── Trader.Backend.Tests/        # Tests for tools, swarm, and risk optimizers
│   ├── NexusBrain.Tests/            # Tests for NexusBrain engine
│   └── Brain.Tests/                 # Tests for BrainSystem modular components
│
├── tools/                           # Developer & Execution Utilities
│   ├── WinRunner/                   # WindowsAppRunner (.NET Avalonia GUI + CLI + Core)
│   └── java-sandbox/                # Java strategy execution sandbox
│
├── engines/                         # Standalone Analytical Engines
│   ├── deriv-ai-swarm/              # Java AI Swarm (500 agents × 1,145 indicators)
│   └── divergence-system/           # Python MTF Divergence Scanner
│
├── libs/                            # High-Performance Libraries
│   ├── jcharts/                     # Java TradingView lightweight charts library
│   └── microkernel/                 # Java high-performance concurrency & memory runtime
│
├── data/                            # Market Datasets
│   └── historical/                  # Historical OHLC CSVs for Forex & Deriv Synthetics
│
├── assets/                          # Static Assets & Media
│   └── fonts/                       # 21 custom typography packages
│
├── scripts/                         # Automation & Helper Scripts
│   └── deriv-api-helpers.js         # Deriv WebSocket & REST API utilities
│
└── docs/                            # Documentation
    ├── ARCHITECTURE.md              # System architecture (this document)
    ├── IMPORT_MANIFEST.md           # Archive import ledger
    ├── fonts/
    │   └── FONTS.md                 # Complete font registry & licensing guide
    └── deriv/                       # Deriv market data & skill documentation
```

---

## Key Subsystems

### 1. TraderApp (.NET MAUI)
The cross-platform client frontend. Supports Android, iOS, macOS, and Windows. It features 6 primary tabs:
- **AI Chat:** Interactive chat with 10+ AI providers (OpenAI, Claude, Gemini, Grok, DeepSeek, etc.).
- **Quotes:** Real-time ticker feeds with favoriting and category filtering (Forex, Crypto, Synthetics).
- **Chart:** High-performance TradingView Lightweight Charts with dynamic indicators.
- **Signals:** AI-graded trading signals with dynamic Stop-Loss / Take-Profit targets.
- **Bot:** Autonomous execution bot tracking live positions and P&L.
- **Settings:** Secure local API key storage.

### 2. Trader.Backend (.NET 8)
A tool-based agentic server exposing 22+ specialized analytical tools:
- `tech.scan`, `market.regime`, `backtest.macross`, `risk.assess`, `portfolio.summary`, `news.sentiment`, `analysis.correlation`, `analysis.volatility`, `analysis.supplydemand`, `analysis.momentum`, `analysis.riskreward`, `analysis.volume`, `swarm.analyze`, `analysis.marketprofile`, `analysis.drawdown`, `analysis.riskmetrics`, `analysis.volsurface`, `analysis.sector`, `analysis.orderflow`, `strategy.evaluate`.

### 3. BrainSystem, DsiAgentic & NexusBrain
Three interconnected cognitive systems providing:
- **Matrix & Tensor Operations:** Native SIMD-accelerated linear algebra.
- **Neural Network Architectures:** Feed-Forward, LSTM, 1D-CNN, and Ensembles.
- **Memory Systems:** Multi-tiered Sensory, Working, Episodic, Semantic, and Procedural memory stores.
- **LLM Runner:** In-process GGUF runner for local language models.
- **Specialist Agent Swarms:** Multi-agent consensus voting on directional bias.

### 4. High-Performance Java & Python Engines
- **Deriv AI Swarm (Java):** 500 specialist agents evaluating 1,145 technical indicators concurrently.
- **Divergence System (Python):** Multi-timeframe pivot analysis and momentum divergence detection.
- **MicroKernel (Java):** Zero-allocation event bus, ring buffers, and fiber-based work stealing.
