# Deriv AI Agent Swarm

## Overview

A native Java AI agent swarm system featuring **500 autonomous agents** and **1,190 technical analysis indicators**, fully integrated with the **Deriv Public API** for real-time market data and trading.

## Architecture

```
┌──────────────────────────────────────────────────────┐
│                  AgentSwarm (500 agents)              │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────┐  │
│  │ 10 Coord.   │  │ 15 Monitor   │  │100 Data     │  │
│  │  Agents     │  │  Agents      │  │  Collectors │  │
│  └──────┬──────┘  └──────┬───────┘  └──────┬──────┘  │
│         │                │                  │         │
│  ┌──────┴────────────────┴──────────────────┴──────┐  │
│  │              MessageBus (Priority Queue)         │  │
│  └──────┬────────────────┬──────────────────┬──────┘  │
│         │                │                  │         │
│  ┌──────┴──────┐  ┌──────┴───────┐  ┌──────┴──────┐  │
│  │150 Analysis │  │100 Signal    │  │ 60 Risk     │  │
│  │  Agents     │  │  Generators  │  │  Managers   │  │
│  └──────┬──────┘  └──────┬───────┘  └──────┬──────┘  │
│         │                │                  │         │
│  ┌──────┴────────────────┴──────────────────┴──────┐  │
│  │     40 Portfolio Agents  │ 25 Execution Agents │  │
│  └────────────────────────┬───────────────────────┘  │
│                           │                          │
│  ┌────────────────────────┴───────────────────────┐  │
│  │         1,190 Technical Indicators             │  │
│  │  Trend │ Momentum │ Volatility │ Volume │ ...   │  │
│  └────────────────────────┬───────────────────────┘  │
│                           │                          │
│  ┌────────────────────────┴───────────────────────┐  │
│  │            Deriv API Client                    │  │
│  │  REST (candles, symbols) + WebSocket (ticks)   │  │
│  └───────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────┘
```

## Agent Distribution

| Type | Count | Description |
|------|-------|-------------|
| Data Collection | 100 | Fetch candle/tick data for 25 symbols x 4+ timeframes |
| Analysis | 150 | Compute technical indicators across 13 categories |
| Signal Generation | 100 | 30+ trading strategies (trend, momentum, mean-reversion) |
| Risk Management | 60 | Position sizing, stop-loss, VaR, drawdown monitoring |
| Portfolio | 40 | Allocation strategies (equal-weight, risk-parity, etc.) |
| Execution | 25 | Order types (market, limit, TWAP, VWAP, etc.) |
| Monitoring | 15 | System health, metrics, alerts, dashboards |
| Coordination | 10 | Orchestration, task distribution, consensus |
| **TOTAL** | **500** | |

## Technical Indicators (1,190)

| Category | Count | Examples |
|----------|-------|----------|
| Trend | 168 | SMA, EMA, DEMA, TEMA, KAMA, HMA, SuperTrend, Ichimoku |
| Pattern | 185 | Doji, Hammer, Engulfing, H&S, Flags, Wedges, Gaps |
| Momentum | 171 | RSI, MACD, Stochastic, CCI, Williams %R, TSI, KST |
| Volume | 104 | OBV, VWAP, MFI, CMF, PVI, NVI, Force Index |
| Volatility | 111 | ATR, Bollinger, Keltner, StdDev, Parkinson, GARCH |
| Support/Resistance | 77 | Pivots (Standard, Woodie, Camarilla, Demark) |
| Statistical | 72 | Z-Score, Skewness, Kurtosis, Sharpe, Hurst |
| Fibonacci | 69 | Retracements, Extensions, Projections, Harmonics |
| Pivot | 51 | Standard, Woodie, Camarilla, Fibonacci, Demark |
| Cycle | 50 | Sine, HT, Ehlers, Wave Trend, Schaff, DPO |
| Market Profile | 46 | POC, VAH/VAL, TPO, Order Blocks, FVG |
| Order Flow | 40 | Delta, CVD, Bid/Ask Imbalance, Absorption |
| Custom/ML | 46 | Kalman, HMM, LSTM, XGBoost, Ensemble |

## Quick Start

### Prerequisites
- Java 17+
- Maven 3.8+

### Build

```bash
mvn clean package -DskipTests
```

### Run

```bash
java -jar target/deriv-ai-swarm-1.0.0.jar
```

### Run Tests

```bash
# File generation validation (fast)
mvn test -Dtest=AgentGenerationTest

# Core framework tests
mvn test -Dtest=CoreFrameworkTest

# Indicator calculation tests
mvn test -Dtest=IndicatorTest

# Deriv API integration tests (requires internet)
mvn test -Dtest=DerivAPITest

# Run all tests
mvn test
```

## Deriv API Integration

Uses Deriv's free public API (no auth required for market data):
- **REST**: Active symbols, candle history, payouts
- **WebSocket**: Real-time tick streaming, live candle updates
- **Default App ID**: 1089 (Deriv test app)

### API Endpoints Used
- `GET /send` - API calls (active_symbols, ticks_history, payout_for_symbol)
- `WSS ws.derivws.com/websockets/v3` - WebSocket for live data

## Key Classes

| Class | Package | Description |
|-------|---------|-------------|
| `Main` | `com.deriv.swarm` | Entry point, runs all phases |
| `AgentSwarm` | `com.deriv.swarm.core` | Swarm orchestrator |
| `Agent` | `com.deriv.swarm.core` | Base agent class |
| `MessageBus` | `com.deriv.swarm.core` | Priority message queue |
| `DerivClient` | `com.deriv.swarm.api` | REST API client |
| `DerivWebSocket` | `com.deriv.swarm.api` | WebSocket client |
| `TechnicalIndicator` | `com.deriv.swarm.indicators` | Indicator interface |
| `IndicatorMath` | `com.deriv.swarm.indicators` | Math utilities |
| `SwarmBuilder` | `com.deriv.swarm` | Reflection-based agent builder |
| `IndicatorRegistry` | `com.deriv.swarm` | Auto-discovers all indicators |

## Configuration

Edit `SwarmConfig.java` to customize:
- Agent counts per type
- Default symbol and timeframe
- Data collection intervals
- Candle history depth
- WebSocket enable/disable

## License

This project is provided for educational and research purposes only. Trading involves substantial risk. Use at your own risk.
