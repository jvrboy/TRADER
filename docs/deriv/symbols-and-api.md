# Deriv API: Complete Symbol & Reference Guide

## Common Forex Pairs

Format: `frx` + currency codes

| Symbol | Description |
|--------|-------------|
| frxEURUSD | Euro / US Dollar |
| frxGBPUSD | British Pound / US Dollar |
| frxUSDJPY | US Dollar / Japanese Yen |
| frxUSDCAD | US Dollar / Canadian Dollar |
| frxAUDUSD | Australian Dollar / US Dollar |
| frxNZDUSD | New Zealand Dollar / US Dollar |
| frxUSDCHF | US Dollar / Swiss Franc |
| frxGBPEUR | British Pound / Euro |
| frxEURGBP | Euro / British Pound |
| frxEURJPY | Euro / Japanese Yen |

### Usage Example
```javascript
// Get EUR/USD price
fetch('https://api.deriv.com/api/v3/', {
  method: 'POST',
  body: JSON.stringify({ ticks: 'frxEURUSD', subscribe: 0 })
})
```

## Cryptocurrencies

Format: `cry` + ticker

| Symbol | Description |
|--------|-------------|
| cryBTC | Bitcoin |
| cryETH | Ethereum |
| cryLTC | Litecoin |
| cryXRP | Ripple |
| cryADA | Cardano |
| crySOL | Solana |
| cryBNB | Binance Coin |
| cryDOGE | Dogecoin |

### Usage Example
```javascript
// Get Bitcoin price
fetch('https://api.deriv.com/api/v3/', {
  method: 'POST',
  body: JSON.stringify({ ticks: 'cryBTC', subscribe: 0 })
})
```

## Commodities & Precious Metals

| Symbol | Description |
|--------|-------------|
| frxXAU | Gold (per oz) |
| frxXAG | Silver (per oz) |
| frxXPD | Palladium (per oz) |
| frxXPT | Platinum (per oz) |

## Synthetic Indices

Format: `R_` + number (Deriv's proprietary indices)

| Symbol | Description |
|--------|-------------|
| R_10 | Volatility 10 Index |
| R_25 | Volatility 25 Index |
| R_50 | Volatility 50 Index |
| R_100 | Volatility 100 Index |
| R_75 | Volatility 75 Index |

## API Endpoints & Requests

### 1. Get Active Symbols
**Purpose**: List all available trading instruments

```javascript
{
  "active_symbols": "brief"  // or "full" for detailed info
}

// Response includes:
// - symbol: trading symbol (e.g., "frxEURUSD")
// - display_name: human-readable name
// - submarket_display_name: category
// - market_display_name: market type
```

**Usage**:
```javascript
const allSymbols = await fetch('https://api.deriv.com/api/v3/', {
  method: 'POST',
  body: JSON.stringify({ active_symbols: 'brief' })
}).then(r => r.json()).then(d => d.active_symbols);

// Filter for crypto
const cryptoSymbols = allSymbols.filter(s => s.symbol.startsWith('cry'));

// Filter for forex
const forexSymbols = allSymbols.filter(s => s.symbol.startsWith('frx'));
```

### 2. Get Ticks (Real-Time Prices)
**Purpose**: Get current or historical price data

```javascript
{
  "ticks": "frxEURUSD",
  "subscribe": 0  // 0 = single request, 1 = subscribe to stream
}

// Response includes:
// - tick.bid: current bid price
// - tick.ask: current ask price
// - tick.epoch: Unix timestamp
// - tick.symbol: the symbol
```

**Usage - Single Price**:
```javascript
const priceData = await fetch('https://api.deriv.com/api/v3/', {
  method: 'POST',
  body: JSON.stringify({ ticks: 'frxEURUSD', subscribe: 0 })
}).then(r => r.json());

if (priceData.tick) {
  console.log(`EUR/USD - Bid: ${priceData.tick.bid}, Ask: ${priceData.tick.ask}`);
}
```

### 3. Get Tick History
**Purpose**: Retrieve historical price data for charting

```javascript
{
  "ticks_history": "frxEURUSD",
  "adjust_start_time": 1,
  "count": 50,
  "end": "latest",
  "style": "ticks"  // or "candles"
}

// Response includes:
// - history.times: array of Unix timestamps
// - history.close: array of closing prices
// - history.open: array of opening prices (if candles)
// - history.high: array of high prices (if candles)
// - history.low: array of low prices (if candles)
```

**Usage - Chart Data**:
```javascript
const history = await fetch('https://api.deriv.com/api/v3/', {
  method: 'POST',
  body: JSON.stringify({
    ticks_history: 'frxEURUSD',
    adjust_start_time: 1,
    count: 50,
    end: 'latest',
    style: 'ticks'
  })
}).then(r => r.json());

const chartData = history.history.times.map((time, i) => ({
  timestamp: new Date(time * 1000),
  price: history.history.close[i]
}));
```

### 4. Get Contracts for Symbol
**Purpose**: See what types of contracts are available for trading

```javascript
{
  "contracts_for": "frxEURUSD"
}

// Response includes available contract types like:
// - CALL (Rise)
// - PUT (Fall)
// - EXPIRYRANGE
// - EXPIRYMISS
// - TOUCH
// - NOTOUCH
```

**Usage**:
```javascript
const contracts = await fetch('https://api.deriv.com/api/v3/', {
  method: 'POST',
  body: JSON.stringify({ contracts_for: 'frxEURUSD' })
}).then(r => r.json());

console.log('Available contract types:', contracts.contracts_for);
```

### 5. Get Assets Information
**Purpose**: Detailed data about all assets

```javascript
{
  "assets": 1
}

// Returns comprehensive asset data
```

### 6. Ping API
**Purpose**: Test connection and check API status

```javascript
{
  "ping": 1
}

// Returns:
// { "ping": "pong" } if successful
```

**Usage**:
```javascript
const isOnline = await fetch('https://api.deriv.com/api/v3/', {
  method: 'POST',
  body: JSON.stringify({ ping: 1 })
}).then(r => r.json())
  .then(d => !d.error)
  .catch(() => false);
```

## Error Handling

### Common Error Responses

```javascript
// Symbol not found
{
  "error": {
    "code": "InvalidSymbol",
    "message": "symbol not found"
  }
}

// Invalid request
{
  "error": {
    "code": "InputValidationFailed",
    "message": "Input validation failed: ..."
  }
}

// API temporarily unavailable
// No response or timeout
```

### Robust Error Handling Pattern

```javascript
async function safeApiCall(request) {
  try {
    const response = await fetch('https://api.deriv.com/api/v3/', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
      timeout: 5000
    });
    
    const data = await response.json();
    
    if (data.error) {
      throw new Error(`API Error: ${data.error.message}`);
    }
    
    return data;
  } catch (error) {
    console.error('Failed to call API:', error.message);
    return { error: error.message };
  }
}
```

## Rate Limiting & Best Practices

- **No strict rate limits** on the public API
- **Be respectful**: Don't hammer the API with requests
- **Batch requests**: Fetch multiple prices in one artifact
- **Cache data**: Store prices locally, update periodically
- **Interval**: 2-5 second refresh is reasonable
- **Batch updates**: If monitoring 10 symbols, fetch all in ~1 second, then update UI

### Recommended Polling Intervals
- Price ticker: 2-5 seconds
- Dashboard: 3-10 seconds
- Historical chart: 10-30 seconds (less frequent updates)
- Symbol list: 60+ seconds (very stable)

## Complete Code Examples

### Example 1: Fetch and Display Single Price

```javascript
async function showPrice(symbol) {
  const res = await fetch('https://api.deriv.com/api/v3/', {
    method: 'POST',
    body: JSON.stringify({ ticks: symbol, subscribe: 0 })
  });
  
  const data = await res.json();
  
  if (data.error) {
    console.log('Error:', data.error.message);
    return null;
  }
  
  return {
    symbol: data.tick.symbol,
    bid: data.tick.bid,
    ask: data.tick.ask,
    spread: (data.tick.ask - data.tick.bid).toFixed(5),
    time: new Date(data.tick.epoch * 1000)
  };
}

// Usage
showPrice('frxEURUSD').then(console.log);
```

### Example 2: Get All Symbols in Category

```javascript
async function getSymbolsByCategory(category) {
  const res = await fetch('https://api.deriv.com/api/v3/', {
    method: 'POST',
    body: JSON.stringify({ active_symbols: 'brief' })
  });
  
  const { active_symbols } = await res.json();
  
  const prefixes = {
    forex: 'frx',
    crypto: 'cry',
    commodities: 'frxX',
    indices: 'R_'
  };
  
  const prefix = prefixes[category];
  return active_symbols.filter(s => s.symbol.startsWith(prefix));
}

// Usage
getSymbolsByCategory('crypto').then(cryptoSymbols => {
  cryptoSymbols.forEach(s => console.log(s.display_name));
});
```

### Example 3: Poll Multiple Prices

```javascript
async function getPricesForSymbols(symbols) {
  const promises = symbols.map(symbol =>
    fetch('https://api.deriv.com/api/v3/', {
      method: 'POST',
      body: JSON.stringify({ ticks: symbol, subscribe: 0 })
    }).then(r => r.json())
  );
  
  const results = await Promise.all(promises);
  
  return results
    .filter(r => r.tick)
    .reduce((acc, r) => {
      acc[r.tick.symbol] = {
        bid: r.tick.bid,
        ask: r.tick.ask
      };
      return acc;
    }, {});
}

// Usage
getPricesForSymbols(['frxEURUSD', 'frxGBPUSD', 'cryBTC']).then(console.log);
```

## WebSocket (Advanced)

For true real-time updates, Deriv also supports WebSocket:

```javascript
const ws = new WebSocket('wss://ws.deriv.com/websockets/v3');

ws.onopen = () => {
  ws.send(JSON.stringify({
    ticks: 'frxEURUSD',
    subscribe: 1
  }));
};

ws.onmessage = (event) => {
  const data = JSON.parse(event.data);
  if (data.tick) {
    console.log('New tick:', data.tick.bid, data.tick.ask);
  }
};
```

This is more efficient for continuous updates but requires WebSocket library support in your artifact.
