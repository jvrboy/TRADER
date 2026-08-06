# Deriv API Market Data Skill

Access real-time market data from Deriv's public API without authentication. Perfect for creating trading dashboards, price tickers, and market analysis tools.

## What This Skill Does

- **Fetch real-time prices** for forex, crypto, commodities, and indices
- **List all available symbols** and their details
- **Get contract information** for any trading instrument
- **Retrieve price history** for charting and analysis
- **Build interactive dashboards** with live data
- **No API key required** - uses public Deriv API

## Quick Start

### Conversational Lookup (Ask in Chat)
```
"What's the price of EUR/USD?"
"Show me Bitcoin price"
"List all available crypto symbols"
```

### Build a Dashboard (Artifacts)
```
"Create a live price dashboard for EUR/USD, GBP/USD, and BTC"
"Make a forex tracker widget"
"Build a crypto price monitor"
```

## Common Use Cases

| Request | What Happens |
|---------|--------------|
| "Get EUR/USD price" | Fetches current bid/ask from Deriv API, displays in chat |
| "Create a price widget" | Builds React artifact with auto-updating price display |
| "Show all crypto symbols" | Fetches active_symbols, filters crypto, displays as list |
| "Build a market dashboard" | Creates multi-symbol dashboard with live prices and charts |
| "What contracts are available for EUR/USD?" | Shows contract types available for trading |
| "Chart EUR/USD" | Fetches price history, displays line chart |

## API Features

### Built-In Endpoints
- `active_symbols` - Get all tradeable instruments
- `ticks` - Get real-time price quotes
- `contracts_for` - Get available contracts
- `ticks_history` - Get historical price data for charts
- `assets` - Get detailed asset information
- `ping` - Test API connectivity

### No Authentication Needed
The Deriv public API doesn't require an API key, so you can:
- Call it directly from artifacts
- Use it in React components
- Fetch data from the browser
- No CORS issues (Deriv API allows it)

## Symbol Guide

### Forex (Currency Pairs)
`frx` + currency codes
- `frxEURUSD` - Euro/Dollar
- `frxGBPUSD` - Pound/Dollar
- `frxUSDJPY` - Dollar/Yen
- [See full list in references/symbols-and-api.md]

### Cryptocurrencies
`cry` + ticker
- `cryBTC` - Bitcoin
- `cryETH` - Ethereum
- `cryLTC` - Litecoin
- [See full list in references/symbols-and-api.md]

### Commodities
- `frxXAU` - Gold
- `frxXAG` - Silver
- `frxXPD` - Palladium

### Synthetic Indices
- `R_10`, `R_25`, `R_50`, `R_100` - Volatility indices

## File Structure

```
deriv-api-market-data/
├── SKILL.md                          # Main skill definition
├── README.md                         # This file
├── scripts/
│   └── deriv-api-helpers.js         # Helper functions
├── references/
│   ├── symbols-and-api.md           # Complete API reference
│   └── artifact-templates.md        # Widget templates
```

## Key Files

### SKILL.md
Main skill definition with:
- When to trigger the skill
- API endpoint reference
- Quick start examples
- Best practices

### references/symbols-and-api.md
**Read this first** for:
- Common symbol list (forex, crypto, commodities)
- Detailed endpoint explanations
- Code examples for each endpoint
- Error handling patterns
- Rate limiting tips

### references/artifact-templates.md
Reference for building:
- Price ticker widgets
- Multi-symbol dashboards
- Symbol explorers
- Contract viewers
- Price charts
- Comparisons

### scripts/deriv-api-helpers.js
Helper functions to simplify common queries:
- `getActiveSymbols(filter)` - List symbols
- `getPrice(symbol)` - Get single price
- `getPrices(symbols)` - Get multiple prices
- `getContracts(symbol)` - Get available contracts
- `getTicks(symbol, count)` - Get price history

## API Base URL

```
https://api.deriv.com/api/v3/
```

All requests:
- Method: POST
- Content-Type: application/json
- No authentication required

## Example Usage

### Get a Price (JavaScript)
```javascript
const response = await fetch('https://api.deriv.com/api/v3/', {
  method: 'POST',
  body: JSON.stringify({
    ticks: 'frxEURUSD',
    subscribe: 0
  })
});

const data = await response.json();
if (data.tick) {
  console.log(`EUR/USD: ${data.tick.bid} / ${data.tick.ask}`);
}
```

### Get All Symbols
```javascript
const response = await fetch('https://api.deriv.com/api/v3/', {
  method: 'POST',
  body: JSON.stringify({
    active_symbols: 'brief'
  })
});

const data = await response.json();
const allSymbols = data.active_symbols;

// Filter to crypto only
const crypto = allSymbols.filter(s => s.symbol.startsWith('cry'));
```

### Get Contract Information
```javascript
const response = await fetch('https://api.deriv.com/api/v3/', {
  method: 'POST',
  body: JSON.stringify({
    contracts_for: 'frxEURUSD'
  })
});

const data = await response.json();
console.log(data.contracts_for); // Available contract types
```

## When This Skill Triggers

Claude will automatically consult this skill when you:
- Ask for live market prices
- Request trading data or symbols
- Ask to create market dashboards
- Want information about contracts
- Need to monitor specific symbols
- Ask for price charts or historical data

## Performance & Refresh Rates

Recommended polling intervals:
- **Price ticker**: 2-5 seconds (most responsive)
- **Dashboard**: 3-10 seconds (balance of freshness and load)
- **Charts**: 10-30 seconds (already historical)
- **Symbol list**: 60+ seconds (very stable)

## Error Handling

If you get errors:
- "Symbol not found" → Check symbol spelling and prefix (frx, cry, etc.)
- "Input validation failed" → Verify request format
- No response → Deriv API may be temporarily down
- Network timeout → API taking too long, increase timeout

## Building Artifacts

When Claude builds a market data artifact for you:
1. **Data fetching** - Fetches from Deriv API in the component
2. **Caching** - Stores data locally to minimize API calls
3. **Auto-refresh** - Updates prices on a timer (2-10s)
4. **Error display** - Shows if API is unavailable
5. **Responsive design** - Works on mobile and desktop

## Advanced Features

### WebSocket for True Real-Time
For continuous price updates, Deriv also supports WebSocket:
```javascript
const ws = new WebSocket('wss://ws.deriv.com/websockets/v3');
```
(More advanced - Claude can help you implement this)

### Historical Price Data
Get OHLC (Open/High/Low/Close) data for charting:
```javascript
{
  "ticks_history": "frxEURUSD",
  "count": 100,
  "style": "candles"  // or "ticks"
}
```

## Support & Questions

If something doesn't work:
1. Check that the symbol format is correct (e.g., `frxEURUSD`, not `EURUSD`)
2. Verify the API is online by asking Claude to "ping the Deriv API"
3. Check the references/symbols-and-api.md for detailed examples
4. Ask Claude: "I'm trying to [task], but getting this error: [error message]"

## Rate Limits

Deriv's public API:
- **No strict rate limits** (but be respectful)
- Recommended: 1-10 requests per second
- Batch requests when possible
- Cache data locally

## License & Attribution

This skill uses Deriv's public API: https://deriv.com/api

Deriv is a legitimate online trading platform. Always verify API availability and terms before building production applications.
