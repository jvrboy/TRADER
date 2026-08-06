---
name: deriv-api-market-data
description: |
  Real-time market data from Deriv's public API. Use this skill whenever the user needs live trading data, market quotes, symbols, contracts, or any trading information from Deriv. Includes built-in templates for dashboards, price widgets, and symbol lookups. Supports both quick conversational lookups and interactive artifacts. Trigger on requests like: "Get EUR/USD price", "Show me active trading symbols", "Create a market data dashboard", "What contracts are available", "Get real-time quotes for..."
---

# Deriv API Market Data Skill

Access real-time trading data from Deriv's Default Public API without authentication. Perfect for market research, price monitoring, contract exploration, and building live trading dashboards.

## Quick Start

The Deriv API uses WebSocket (for real-time data) and HTTP endpoints. For simple queries, we'll use HTTP requests to:
- **Get active symbols** - all tradeable instruments
- **Get prices** - real-time quotes for any symbol
- **Get contracts** - available trading contracts
- **Get ticks** - historical price data
- **Get assets** - detailed information about instruments

## Common Use Cases

### 1. Get Real-Time Price (Conversational)
```javascript
const response = await fetch('https://api.deriv.com/api/v3/', {
  method: 'POST',
  body: JSON.stringify({
    ticks: "frxEURUSD",  // Any valid symbol
    subscribe: 1
  })
});
```

### 2. List All Active Symbols
```javascript
const response = await fetch('https://api.deriv.com/api/v3/', {
  method: 'POST',
  body: JSON.stringify({ active_symbols: "brief" })
});
```

### 3. Get Contract Details
```javascript
const response = await fetch('https://api.deriv.com/api/v3/', {
  method: 'POST',
  body: JSON.stringify({
    contracts_for: "frxEURUSD"
  })
});
```

## API Endpoints

### Main Endpoint
- **Base URL**: `https://api.deriv.com/api/v3/`
- **Method**: POST
- **Content-Type**: application/json
- **No authentication required** (public API)

### Key Requests

| Request Type | Purpose | Example |
|---|---|---|
| `active_symbols` | Get all tradeable instruments | `"active_symbols": "brief"` |
| `contracts_for` | Get available contracts for a symbol | `"contracts_for": "frxEURUSD"` |
| `ticks` | Get real-time or historical ticks | `"ticks": "frxEURUSD"` |
| `assets` | Get detailed asset information | `"assets": 1` |
| `ping` | Test API connection | `"ping": 1` |

## Available Symbol Prefixes

- **Forex**: `frxEUR`, `frxGBP`, `frxAUD`, etc. (currency pairs)
- **Crypto**: `cryBTC`, `cryETH`, `cryLTC`, etc.
- **Commodities**: `frxXAU` (gold), `frxXAG` (silver)
- **Indices**: `R_10`, `R_25`, `R_50`, etc. (synthetic indices)
- **Stocks**: Various stock symbols

## Building Interactive Artifacts

When the user asks for a dashboard or widget, use React with:

1. **Fetch market data from Deriv API**
2. **Display in real-time chart** (use recharts or similar)
3. **Include price updates** (optional WebSocket for true real-time)
4. **Add interactive controls** (symbol selector, timeframe chooser)

For artifacts, avoid repeated API calls — use React state to cache data and update on intervals.

## Common Patterns

### Quick Lookup (Conversational)
User: "What's the price of EUR/USD?"
→ Fetch current tick for `frxEURUSD`
→ Display price in chat

### Dashboard (Artifact)
User: "Create a live market dashboard"
→ Build React component
→ Fetch symbols and prices
→ Display with charts and updates

### Symbol Explorer (Artifact)
User: "Show me all crypto symbols available"
→ Call `active_symbols` with crypto filter
→ Display as interactive list with prices

## Error Handling

Common responses:
- `error: "Input validation failed"` → Check symbol spelling
- `error: "Symbol not found"` → Symbol doesn't exist (verify prefix)
- No response → Deriv API may be temporarily unavailable

Always verify the symbol format before querying. Use `active_symbols` to see available options.

## Rate Limiting & Best Practices

- No strict rate limits on public API (but be respectful)
- Batch requests when possible
- Cache data locally in artifacts (don't re-fetch every second)
- Use WebSocket for true real-time (more efficient)

## Example: All Available Currencies

```javascript
await fetch('https://api.deriv.com/api/v3/', {
  method: 'POST',
  body: JSON.stringify({ active_symbols: "brief" })
}).then(r => r.json())
  .then(data => console.log(data.active_symbols))
```

---

## When to Use This Skill

✓ User wants live market prices  
✓ User needs a trading data dashboard  
✓ User asks "What contracts are available?"  
✓ User wants to monitor specific symbols  
✓ User needs symbol/asset information  
✓ User asks for real-time price widgets  

## When NOT to Use

✗ User wants to place trades (need authenticated API)  
✗ User needs account-specific data (need authentication)  
✗ User wants historical analysis (beyond tick data)
