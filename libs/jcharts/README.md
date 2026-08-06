# JCharts - Lightweight TradingView Charts in Java

A native Java (Java 2D/Swing) lightweight TradingView-style charting library.

## What's Inside

### 17 Chart Types
| # | Type | Category |
|---|------|----------|
| 1 | Candlestick | Standard |
| 2 | Line | Standard |
| 3 | OHLC Bar | Standard |
| 4 | Area | Standard |
| 5 | Hollow Candle | Standard |
| 6 | Heikin Ashi | Standard |
| 7 | Renko | Advanced |
| 8 | Kagi | Advanced |
| 9 | Point & Figure | Advanced |
| 10 | Line Break | Advanced |
| 11 | Equivolume | Advanced |
| 12 | Percentage | Advanced |
| 13 | Volume Heatmap | Original |
| 14 | Momentum Wave | Original |
| 15 | Flow Force | Original |
| 16 | Elastic Band | Original |
| 17 | Quantum Ribbon | Original |

### 15 Drawing Tools
Trend Line, Horizontal Line, Vertical Line, Rectangle, Ellipse,
Fibonacci Retracement, Fibonacci Fan, Fibonacci Timezone,
Text Annotation, Arrow, Measurement, Channel, Pitchfork, Brush

### 9 Technical Indicators
SMA, EMA, RSI, MACD, Bollinger Bands, Stochastic, ATR, VWAP, Volume Profile

### Features
- Zoom/Pan (mouse wheel + drag)
- Keyboard shortcuts (arrows, +/-, G, V, C, L, Home, End)
- Crosshair with price/time labels
- Volume overlay
- Timeframe conversion (1m to 1M)
- Multi-panel layouts
- Chart templates (save/restore)
- Price alerts
- Data replay (play/pause/step)
- Symbol comparison (normalized %)
- CSV/JSON export & import
- PNG screenshot
- Grid, legend, price/time axes
- Auto and manual price scaling

## Build

With Maven:
```bash
mvn clean package
```

Without Maven (JDK 11+):
```bash
javac -d target/classes -sourcepath src/main/java $(find src/main/java -name '*.java')
java -cp target/classes com.jcharts.ChartDemo
```

## Run Demo

```bash
java -cp target/classes com.jcharts.ChartDemo
```

## Test

86 JUnit 5 tests covering all components.

```bash
mvn test
```

## Project Structure

```
src/main/java/com/jcharts/
  core/          - ChartPanel, ChartModel, ChartRenderer, ChartColor
  data/          - OHLCBar, TimeSeries
  types/         - 12 standard + advanced chart renderers
  types/original/- 5 original chart renderers
  tools/         - 15 drawing tools
  indicators/    - 9 technical indicators
  features/      - ZoomPan, Timeframe, MultiPanel, Template, Alert, Replay, Compare, Export
  util/          - Math utilities
  ChartDemo.java - Comprehensive demo application

src/test/java/com/jcharts/
  8 test classes, 86 test methods total
```

## Requirements
- Java 11+
- No external dependencies (pure Java AWT/Swing)

## License
MIT
