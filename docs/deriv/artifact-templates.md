# Deriv API Artifact Templates

Ready-to-use patterns for building interactive market data widgets and dashboards.

## 1. Live Price Ticker Widget

Display real-time price for a single symbol with bid/ask spread.

**When to suggest**: User asks "Show me EUR/USD price", "Create a price widget"

```jsx
// Minimal example - adapt in artifacts based on user needs
const [price, setPrice] = useState(null);
const [symbol] = useState('frxEURUSD');

useEffect(() => {
  const fetchPrice = async () => {
    const res = await fetch('https://api.deriv.com/api/v3/', {
      method: 'POST',
      body: JSON.stringify({ ticks: symbol, subscribe: 0 })
    });
    const data = await res.json();
    if (data.tick) setPrice(data.tick);
  };
  
  fetchPrice();
  const interval = setInterval(fetchPrice, 2000);
  return () => clearInterval(interval);
}, [symbol]);

return price ? (
  <div style={{padding: '20px', fontSize: '18px'}}>
    <h3>{symbol}</h3>
    <p>Bid: {price.bid}</p>
    <p>Ask: {price.ask}</p>
    <p>Spread: {(price.ask - price.bid).toFixed(5)}</p>
  </div>
) : <p>Loading...</p>;
```

## 2. Multi-Symbol Price Dashboard

Show prices for multiple symbols in a table.

**When to suggest**: "Create a market dashboard", "Monitor these symbols"

```jsx
// Layout: Table of symbols with live prices
// Key features:
// - List of symbols to monitor (hardcoded or selectable)
// - Fetch all prices on load
// - Update every 3-5 seconds
// - Show bid/ask/change indicator
// - Color-code up/down moves

Example symbols to monitor:
- frxEURUSD, frxGBPUSD, frxUSDJPY (forex)
- cryBTC, cryETH (crypto)
- frxXAU (gold)
```

## 3. Symbol Explorer / Watchlist

Browse all available symbols filtered by category.

**When to suggest**: "Show me all crypto symbols", "What forex pairs are available?"

```jsx
// Features:
// - Fetch active_symbols
// - Filter by type (Forex, Crypto, Indices, Stocks)
// - Display as sortable list or grid
// - Click symbol to view details or add to watchlist
// - Show symbol name, display name, current price
```

## 4. Contract Details Viewer

Show available contracts for a selected symbol.

**When to suggest**: "What contracts can I trade for EUR/USD?", "Show contract types"

```jsx
// Features:
// - Symbol selector (input or dropdown)
// - Fetch contracts_for selected symbol
// - Display available contract types:
//   - Rise/Fall
//   - Higher/Lower
//   - Touches/No Touch
//   - etc.
// - Show details like payout, barrier levels, duration
```

## 5. Price Chart (Recharts Integration)

Line chart showing recent price movement.

**When to suggest**: "Chart EUR/USD for me", "Show price history", "Visualize symbol movement"

```jsx
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend } from 'recharts';

// Features:
// - Fetch ticks_history for symbol
// - Transform to chart data points
// - Animate price changes
// - Show bid/ask lines
// - Timeframe selector (optional)
// - Auto-refresh feature
```

## 6. Forex Pair Comparison

Compare multiple currency pairs side-by-side.

**When to suggest**: "Compare EUR/USD and GBP/USD", "Show forex pairs"

```jsx
// Features:
// - Grid of forex pairs
// - Side-by-side price display
// - Highlight winners/losers
// - Sort by change %, bid/ask, etc.
// - Quick selection of major pairs
```

## 7. Crypto Tracker

Dedicated widget for cryptocurrency prices.

**When to suggest**: "Show crypto prices", "Create a crypto dashboard"

```jsx
// Features:
// - List all crypto symbols (cry*)
// - Display price, bid/ask spread
// - Show 24h change (if available)
// - Color gradient: green up, red down
// - Refresh button
```

## 8. Market Status Indicator

Simple widget showing API status and market conditions.

**When to suggest**: "Is the market open?", "Check market status"

```jsx
// Features:
// - Ping API to check connectivity
// - Show timestamp of last update
// - Display active markets/symbols count
// - Visual indicator (green = online, red = offline)
```

## Common Patterns

### Symbol Formatting
- Forex: `frx` + currency code (e.g., `frxEURUSD`)
- Crypto: `cry` + ticker (e.g., `cryBTC`)
- Indices: `R_` + number (e.g., `R_50`)
- Commodities: `frx` + code (e.g., `frxXAU` for gold)

### Data Caching
```javascript
const cache = useRef({});
const fetchWithCache = async (symbol) => {
  if (cache.current[symbol]) return cache.current[symbol];
  const data = await fetchPrice(symbol);
  cache.current[symbol] = data;
  return data;
};
```

### Error Handling
```javascript
try {
  const data = await fetch(...).then(r => r.json());
  if (data.error) throw new Error(data.error.message);
  return data;
} catch (e) {
  setError(`Failed to fetch: ${e.message}`);
}
```

### Auto-Refresh Pattern
```javascript
const [refreshInterval, setRefreshInterval] = useState(2000);

useEffect(() => {
  const timer = setInterval(fetchData, refreshInterval);
  return () => clearInterval(timer);
}, [refreshInterval]);
```

## Quick API Call Examples

### Single Price Lookup
```javascript
const res = await fetch('https://api.deriv.com/api/v3/', {
  method: 'POST',
  body: JSON.stringify({ ticks: 'frxEURUSD', subscribe: 0 })
});
const { tick } = await res.json();
```

### Get All Symbols
```javascript
const res = await fetch('https://api.deriv.com/api/v3/', {
  method: 'POST',
  body: JSON.stringify({ active_symbols: 'brief' })
});
const { active_symbols } = await res.json();
```

### Get Contracts
```javascript
const res = await fetch('https://api.deriv.com/api/v3/', {
  method: 'POST',
  body: JSON.stringify({ contracts_for: 'frxEURUSD' })
});
const contractData = await res.json();
```

## Performance Tips

1. **Batch requests** - Fetch multiple symbols in parallel, not series
2. **Cache data** - Store prices locally, update via intervals
3. **Debounce** - Don't re-fetch on every keystroke
4. **Use ref for intervals** - Properly cleanup setInterval
5. **Minimize re-renders** - Keep state updates focused
6. **WebSocket option** - For true real-time, use WebSocket (more advanced)

## Styling Tips

- Use CSS variables for theme consistency
- Color code: Green for up, red for down
- Monospace font for numbers (easier to scan)
- Add loading spinners for better UX
- Show last update timestamp
- Responsive design for mobile compatibility
