# TRADER — Unified Cross-Platform AI Trading Platform

> A production-grade, multi-engine algorithmic trading ecosystem and cross-platform native **.NET MAUI** application supporting **Android**, **iOS**, **macOS**, and **Windows**.

---

## Architecture Overview

`TRADER` is structured as a unified monorepo integrating cross-platform clients, an agentic tool backend, deep learning cognitive engines, specialist swarm networks, historical data suites, and typography systems.

```
TRADER/
├── TRADER.sln                       # Master Visual Studio / Rider solution
├── README.md                        # Monorepo documentation
├── .gitignore                       # Multi-stack build ignore
│
├── src/                             # .NET C# Source Projects
│   ├── TraderApp/                   # .NET MAUI cross-platform UI app (6 tabs)
│   ├── Trader.Backend/              # Agentic tool server (31 specialist trading tools & Swarm)
│   ├── BrainSystem/                 # Cognitive neural network subsystem
│   │   ├── Native/                  # Native GGUF runner, tensors & knowledge graphs
│   │   ├── Api/                     # ASP.NET Core API server
│   │   └── Modular/                 # Modular cognitive layers (Core, Memory, LLM, Tools, Training)
│   ├── DsiAgentic/                  # 14-layer agentic market analysis system
│   ├── NexusBrain/                  # Self-learning agentic brain & Forex analyzer
│   ├── AiBrain/                     # Multi-memory neural learning framework
│   └── AetherBrain/                 # Adaptive weight & multi-agent orchestrator
│
├── tests/                           # Unit & Integration Test Suites
│   ├── Trader.Backend.Tests/        # xUnit tests for 31 tools, swarm & risk models
│   ├── NexusBrain.Tests/            # NexusBrain test suite
│   └── Brain.Tests/                 # Cognitive engine tests
│
├── tools/                           # Developer & Runner Utilities
│   ├── WinRunner/                   # WindowsAppRunner (.NET Avalonia GUI + CLI + Core)
│   └── java-sandbox/                # Java strategy execution sandbox
│
├── engines/                         # Standalone Analytical Engines
│   ├── deriv-ai-swarm/              # Java AI Swarm (500 agents × 1,145 indicators)
│   └── divergence-system/           # Python Multi-Timeframe Divergence Scanner
│
├── libs/                            # High-Performance Core Libraries
│   ├── jcharts/                     # Java TradingView lightweight charts library
│   └── microkernel/                 # Java high-performance microkernel (Fibers & Ring Buffers)
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
└── docs/                            # Comprehensive Documentation
    ├── ARCHITECTURE.md              # Detailed Monorepo architecture guide
    ├── IMPORT_MANIFEST.md           # Archive extraction & integrity manifest
    ├── fonts/
    │   └── FONTS.md                 # Complete font registry & licensing guide
    └── deriv/                       # Deriv market data & skill guides
```

---

## Core Features & Frontend Navigation

`src/TraderApp` is built on **.NET MAUI (C#)** and delivers 6 main pages:

| Tab | Feature Highlights |
|-----|-------------------|
| **AI Chat** | Multi-provider streaming AI trading assistant (OpenAI, Claude, Gemini, Grok, DeepSeek, Mistral, Cohere, Together, Perplexity). |
| **Quotes** | Real-time ticker feeds covering Forex Majors, Cryptocurrencies, Synthetic Indices, Metals, and Stock Indices. |
| **Chart** | Interactive TradingView Lightweight Charts with dynamic overlays and 60+ technical indicators. |
| **Signals** | AI-generated buy/sell signals with calculated Entry, Stop-Loss, Take-Profit targets, and confidence grading. |
| **Bot** | Autonomous execution bot tracking active orders, open positions, execution logs, and live P&L. |
| **Settings** | Secure, on-device API key vault for 10+ AI model providers and 7+ broker data streams. |

---

## Backend Agentic Tools (`src/Trader.Backend`)

The backend exposes a tool registry and autonomous agent runtime equipped with **31 financial & quantitative analysis tools**:

| Tool | Identifier | Purpose |
|------|------------|---------|
| **Technical Scanner** | `tech.scan` | Multi-indicator scoring across RSI, EMA trends, and ATR. |
| **Fibonacci Analysis** | `analysis.fibonacci` | Swing High/Low retracements (23.6%, 38.2%, 50%, 61.8% Golden Pocket) & extensions. |
| **Harmonic Patterns** | `analysis.harmonic` | Classical harmonic pattern scanner (Gartley, Bat, Butterfly, Crab) and PRZ targets. |
| **Multi-Timeframe Trend** | `analysis.mtf` | Short, medium, and long-term trend alignment and confluence scoring. |
| **Smart Money Concepts** | `analysis.smc` | Fair Value Gaps (FVG), Buy/Sell-side liquidity pools, and Premium/Discount pricing. |
| **Pivot Points** | `analysis.pivots` | Classic, Fibonacci, Camarilla (H1-H5, L1-L5), and Woodie pivot levels. |
| **Position Sizing** | `risk.positionsize` | Optimal position sizing via Fixed-Risk, Full Kelly, and Half-Kelly criterion. |
| **Monte Carlo Simulation** | `risk.montecarlo` | 500+ path equity simulation computing Risk of Ruin and max drawdown percentiles. |
| **Options Greeks** | `analysis.greeks` | Black-Scholes European option pricing, Delta, Gamma, Theta, Vega, and Rho. |
| **Elliott Wave** | `analysis.elliottwave` | Elliott Wave Oscillator (EWO) cycle phase estimation and momentum peaks. |
| **Statistical Arbitrage** | `analysis.arbitrage` | Pair spread cointegration, hedge ratio (Beta), and rolling Z-score mean-reversion. |
| **Market Regime** | `market.regime` | Trend and volatility regime classification. |
| **MACross Backtest** | `backtest.macross` | EMA-crossover backtesting engine calculating win-rates & returns. |
| **Risk Assessment** | `risk.assess` | Exposure, concentration, and drawdown risk budgeting. |
| **Portfolio Optimizer** | `portfolio.summary` | Kelly-criterion asset allocation across candidate signals. |
| **News Sentiment** | `news.sentiment` | Natural language market sentiment extraction. |
| **Correlation Matrix** | `analysis.correlation` | Pearson correlation of returns between asset pairs. |
| **Volatility Surface** | `analysis.volsurface` | Implied and historical volatility surfaces with skew models. |
| **Supply / Demand** | `analysis.supplydemand` | Dynamic support and resistance pivot discovery. |
| **Momentum Composite** | `analysis.momentum` | Multi-timeframe momentum scoring (ROC + RSI + Trend). |
| **Risk / Reward** | `analysis.riskreward` | Trade quality scoring and risk-to-reward ratio analysis. |
| **Volume Profile** | `analysis.marketprofile` | Point of Control (POC), Value Area High (VAH) / Low (VAL). |
| **Drawdown Analytics** | `analysis.drawdown` | Maximum drawdown and recovery duration computation. |
| **Sharpe & Sortino** | `analysis.riskmetrics` | Risk-adjusted return metrics calculation. |
| **Sector Rotation** | `analysis.sector` | Relative strength ranking across asset sectors. |
| **Order Flow** | `analysis.orderflow` | Order-book bid/ask imbalance and absorption pressure. |
| **Swarm Consensus** | `swarm.analyze` | Multi-agent specialist consensus voting engine. |
| **Strategy Evaluator** | `strategy.evaluate` | Declarative multi-condition rule evaluator. |
| **Volatility Range** | `analysis.volatility` | Realized volatility, ATR, and volatility regime. |
| **Volume Dynamics** | `analysis.volume` | Relative volume (RVOL) and volume-price trend confirmation. |
| **Scheduler Plan** | `scheduler.plan` | Recommended execution cadence and task intervals. |

---

## Typography & Fonts (`assets/fonts/`)

The repository includes 21 curated font packages with full license and metadata files located in `assets/fonts/`:

| Family | Formats | License | Primary Use |
|--------|---------|---------|-------------|
| **Aiden** | OTF | Freeware, Non-Commercial | Display headers |
| **Ariana Violeta** | TTF | Freeware | UI accents |
| **Baby Plums** | TTF | Freeware, Non-Commercial | Themed displays |
| **Becky Tahlia** | TTF | Freeware | Clean headlines |
| **Believe It** | TTF | Freeware | Promotional callouts |
| **Branda** | TTF | Freeware, Non-Commercial | Brand styling |
| **Brownie Stencil** | TTF | Freeware, Non-Commercial | Industrial labels |
| **Chrusty Rock** | TTF | Demo | Badge labels |
| **Conquest** | TTF | Demo | Hero banners |
| **Cookie Crisp** | TTF | Freeware, Non-Commercial | Card titles |
| **Debrosee** | TTF | Demo | Editorial badges |
| **Freedom** | OTF / TTF | CC BY-SA (Creative Commons) | Full open distribution / App branding |
| **Glorious Free** | TTF | Demo | Feature callouts |
| **Happy Swirly** | TTF | Freeware, Non-Commercial | Custom UI themes |
| **Inflate PTX** | TTF | Demo | 3D / Bold headlines |
| **Love Days** | TTF | Freeware, Non-Commercial | Stylized badges |
| **Playful Time** | TTF | Freeware, Non-Commercial | Interactive badges |
| **Shiny Crystal** | TTF | Freeware, Non-Commercial | Stat highlights |
| **Short Baby** | TTF | Freeware | Clean compact text |
| **To The Point** | TTF | SIL Open Font License (OFL) | Full open distribution / Number callouts |
| **Winter Song** | TTF | Demo | Script typography |

*See [`docs/fonts/FONTS.md`](docs/fonts/FONTS.md) for full licensing details and attribution.*

---

## Build Instructions

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [.NET MAUI Workload](https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation)

### Build the Unified Solution
```bash
dotnet build TRADER.sln
```

### Build & Run the Backend
```bash
dotnet run --project src/Trader.Backend
```

### Build the Mobile & Desktop Client
```bash
# Android
dotnet build src/TraderApp/TraderUI.csproj -f net8.0-android

# Windows
dotnet build src/TraderApp/TraderUI.csproj -f net8.0-windows10.0.19041.0

# iOS / macOS
dotnet build src/TraderApp/TraderUI.csproj -f net8.0-ios
dotnet build src/TraderApp/TraderUI.csproj -f net8.0-maccatalyst
```

### Run Tests
```bash
dotnet test tests/Trader.Backend.Tests
dotnet test tests/NexusBrain.Tests
dotnet test tests/Brain.Tests
```

---

## License

MIT License — Free to use, modify, and distribute.
