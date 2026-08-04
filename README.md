# TRADER — Native C# .NET MAUI Trading App

> A full-featured, cross-platform trading application built with .NET MAUI (C#), supporting **Android**, **iOS**, **macOS**, and **Windows** from a single codebase.

---

## Overview

TRADER is a professional-grade AI-powered trading platform that integrates **500 AI agents**, **1,145 technical indicators**, and **12 backend analysis modules** into a sleek, dark-themed native app.

---

## Features

### 6 Main Tabs

| Tab | Description |
|-----|-------------|
| **AI Chat** | Multi-provider AI assistant (OpenAI, Claude, Gemini, Grok, DeepSeek, Mistral, Cohere, Together, Perplexity) |
| **Quotes** | Real-time market quotes for Forex, Crypto, Indices, Stocks, Synthetics with favorites |
| **Chart** | Interactive TradingView Lightweight Charts with 60+ live indicators |
| **Signals** | AI-generated trading signals with entry, SL, TP levels and confidence scores |
| **Bot** | Automated trading bot with live P&L, open positions, and trade history |
| **Settings** | Full API key management for 10+ AI providers and 7+ broker/data APIs |

---

## Backend Tools (All Ported to C#)

| Module | Origin | Description |
|--------|--------|-------------|
| **AI Brain** | `ai_brain_tool/` | Self-improving neural network with adaptive learning |
| **ChartSight** | `chartsight-python-tool/` | Chart pattern recognition and analysis |
| **Divergence System** | `divergence-system/` | RSI/MACD divergence detection across timeframes |
| **Nova Brain** | `divergence-system/nova_brain.py` | AI planning and composite scoring engine |
| **Drift Switch Lab** | `drift-switch-lab/` | EMA/RSI/Breakout strategy signal generator |
| **Synthetics Analysis** | `synthetics_analysis/` | Synthetic index OHLC generation and backtesting |
| **Deriv AI Swarm** | `deriv-ai-swarm-500agents-1145indicators/` | 500 agents × 1,145 indicators consensus engine |
| **MicroKernel System** | `microkernel-system-v1.0.0/` | 15 high-performance computation kernels |
| **Java Sandbox** | `java-sandbox/` | Secure strategy execution environment |
| **JCharts Library** | `jcharts-tradingview-library/` | Advanced charting with indicators |
| **NEXUS System** | `divergence-system/nexus_files/` | Unified multi-timeframe analysis framework |
| **Deriv API** | `synthetics_analysis/deriv_api_helpers.js` | WebSocket-based live trading integration |

---

## Architecture

```
TRADER/
├── TraderUI/                    # .NET MAUI cross-platform app
│   ├── Models/                  # Data models (Quote, Signal, BotTrade, etc.)
│   ├── ViewModels/              # MVVM ViewModels for all pages
│   ├── Views/                   # XAML pages (6 tabs + 4 detail pages)
│   ├── Services/                # All backend services
│   │   ├── IServices.cs         # Service interfaces
│   │   ├── LocalStorageService  # JSON file persistence
│   │   ├── SettingsService      # API key management
│   │   ├── MarketDataService    # Real-time quotes + OHLC
│   │   ├── AiChatService        # Multi-provider AI chat
│   │   ├── IndicatorService     # 60+ technical indicators
│   │   ├── AnalysisServices     # Divergence, DriftLab, Synthetics, AI Brain
│   │   └── TradingServices      # Signals, Bot, Chart Analysis, Swarm, Deriv
│   ├── Converters/              # XAML value converters
│   └── Resources/               # Fonts, colors, styles, icons
└── TraderBackend/               # Optional .NET backend server
```

---

## Supported AI Providers

- **OpenAI** — GPT-4o, GPT-4 Turbo, GPT-3.5
- **Anthropic** — Claude 3.5 Sonnet, Claude 3 Opus
- **Google Gemini** — Gemini 1.5 Pro, Gemini Flash
- **xAI Grok** — Grok Beta, Grok Vision
- **DeepSeek** — DeepSeek Chat, DeepSeek Reasoner
- **Mistral AI** — Mistral Large, Mixtral 8x7B
- **Cohere** — Command R+, Command R
- **Together AI** — Llama 3, Mixtral, 50+ open models
- **Perplexity** — Sonar Large (real-time web search)
- **HuggingFace** — Thousands of open-source models

---

## Supported Brokers & Data APIs

- **Deriv** — Live trading, Synthetics, Forex, Crypto
- **TwelveData** — Real-time OHLC for stocks, forex, crypto
- **Alpha Vantage** — Stocks, forex, economic data
- **Binance** — Crypto trading and market data
- **Polygon.io** — US stocks, options, forex, crypto
- **Finnhub** — Real-time stocks, news, earnings
- **CoinGecko** — Crypto prices, market cap, DeFi

---

## Building the App

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [.NET MAUI workload](https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation)

```bash
dotnet workload install maui
```

### Build for Android

```bash
cd TraderUI
dotnet build -f net8.0-android
```

### Build for Windows

```bash
cd TraderUI
dotnet build -f net8.0-windows10.0.19041.0
```

### Build for iOS/macOS (requires macOS)

```bash
cd TraderUI
dotnet build -f net8.0-ios
dotnet build -f net8.0-maccatalyst
```

### Publish (Release)

```bash
dotnet publish -f net8.0-android -c Release
dotnet publish -f net8.0-windows10.0.19041.0 -c Release
```

---

## Local Data Storage

All data is stored locally on the device using JSON files in the app's data directory:

- `settings.json` — API keys and preferences
- `signals_*.json` — Signal history
- `trades_*.json` — Bot trade history
- `quotes_cache.json` — Cached quotes
- `chat_history.json` — AI chat history
- `ai_brain_*.json` — Neural network task states

---

## Technical Indicators (60+ implemented)

**Trend:** EMA 8/21/50/200, SMA 20/50, WMA, HMA, DEMA, TEMA, KAMA, ZLEMA, T3, VWMA

**Momentum:** RSI 7/14/21, StochRSI, MACD, Momentum, ROC, CCI, Williams %R, Ultimate Oscillator, TSI, DPO, PPO

**Volatility:** Bollinger Bands, ATR, Keltner Channels, Donchian Channels, Historical Volatility, Chaikin Volatility

**Volume:** OBV, VWAP, MFI, ADL, CMF, Force Index, EOM, Volume Oscillator

**Trend Strength:** ADX, DI+/DI-, Aroon, Parabolic SAR, Ichimoku (all components)

**Oscillators:** Stochastic %K/%D, Awesome Oscillator, CMO, Fisher Transform, Elder Ray

**Special:** RSI Divergence, MACD Divergence, Drift Signal, Breakout Signal, Volatility Index, Nova Brain Score, Swarm Consensus

---

## License

MIT License — Free to use, modify, and distribute.

---

*Built with ❤️ using .NET MAUI, C#, and the power of 500 AI agents.*
