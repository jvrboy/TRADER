# 🚀 Deriv API Market Data Skill - Quick Start

Your **Deriv API Market Data Skill** is ready to use! This skill gives you instant access to real-time market data without any API keys.

## What You Just Got

✅ **Complete Deriv API Integration**
- Real-time forex, crypto, commodities, indices prices
- No authentication required (public API)
- Ready for dashboards, charts, price tickers
- Pre-built templates for common widgets

✅ **Multiple Files & References**
- `SKILL.md` - Main skill definition
- `README.md` - Overview & examples
- `references/symbols-and-api.md` - **Complete API guide (START HERE!)**
- `references/artifact-templates.md` - Widget templates
- `scripts/deriv-api-helpers.js` - Helper functions

## 🎯 How to Use It

### Option 1: Quick Price Lookup (in Chat)
Just ask Claude:
```
"What's the EUR/USD price?"
"Show me Bitcoin price right now"
"List all available crypto symbols"
```

Claude will automatically fetch from Deriv and display it.

### Option 2: Build Interactive Dashboards (Artifacts)
Ask Claude to create something visual:
```
"Create a live price widget for EUR/USD"
"Build a forex tracker showing EUR/USD, GBP/USD, and USD/JPY"
"Make a crypto price dashboard with BTC, ETH, and LTC"
"Show me a chart of EUR/USD prices"
```

Claude will build an interactive React widget that updates in real-time.

### Option 3: Custom Queries
Ask Claude to help you fetch and display specific data:
```
"Get me prices for these symbols: frxEURUSD, cryBTC, frxXAU"
"Show contracts available for EUR/USD"
"What trading symbols are available?"
```

## 📚 Key References

| File | What's Inside | When to Read |
|------|---------------|--------------|
| `README.md` | Overview, quick start, use cases | First time setup |
| `references/symbols-and-api.md` | **All symbols, API endpoints, code examples** | When you need specific data |
| `references/artifact-templates.md` | Widget templates, patterns, styling | When building dashboards |
| `scripts/deriv-api-helpers.js` | Pre-written helper functions | For custom integrations |

## 🎓 Learning Path

### 1. Understand What's Available
- Read `README.md` (5 min)
- Skim `references/symbols-and-api.md` - Symbol section (5 min)

### 2. Try Some Queries
```
"What symbols can I trade?"
"Show me EUR/USD price"
"Get Bitcoin and Ethereum prices"
"What contracts are available for Bitcoin?"
```

### 3. Build Your First Dashboard
```
"Create a price ticker for EUR/USD that updates every 2 seconds"
"Make a dashboard showing these 3 symbols: frxEURUSD, frxGBPUSD, cryBTC"
"Build a chart showing EUR/USD price movement"
```

### 4. Customize & Extend
Once you see how it works, ask Claude to modify:
```
"Add more symbols to the dashboard"
"Change the chart to show candlesticks instead of line"
"Add bid/ask spread calculation"
"Make it show percentage change"
```

## 💡 Common Symbols to Start With

### Forex (Currency Pairs)
- `frxEURUSD` - Euro/Dollar
- `frxGBPUSD` - Pound/Dollar  
- `frxUSDJPY` - Dollar/Yen

### Crypto
- `cryBTC` - Bitcoin
- `cryETH` - Ethereum
- `cryLTC` - Litecoin

### Commodities
- `frxXAU` - Gold
- `frxXAG` - Silver

### Indices
- `R_50` - Volatility 50 Index

## 🔧 Technical Details

**API Endpoint**: `https://api.deriv.com/api/v3/`

**No Authentication**: Public API - works from browser/artifacts directly

**Main Operations**:
- Get prices: `{ "ticks": "frxEURUSD", "subscribe": 0 }`
- List symbols: `{ "active_symbols": "brief" }`
- Get contracts: `{ "contracts_for": "frxEURUSD" }`
- Get history: `{ "ticks_history": "frxEURUSD", "count": 50 }`

**Refresh Rate**: 2-10 seconds recommended (no strict rate limits)

## ❓ Troubleshooting

**Q: "Symbol not found" error**
- Check symbol format (e.g., `frxEURUSD`, not `EURUSD`)
- Use `active_symbols` to see valid symbols
- Verify prefix: `frx` (forex), `cry` (crypto), `R_` (indices)

**Q: No data showing**
- Deriv API might be temporarily down (rare)
- Check your internet connection
- Try the ping request to test connectivity

**Q: How do I get historical data?**
- Use `ticks_history` endpoint with count parameter
- See examples in `references/symbols-and-api.md`

**Q: Can I trade with this?**
- No, this is read-only public API
- For trading, you need authentication (separate Deriv account)
- This skill is for **price data and analysis only**

## 📖 Next Steps

1. **Read** `references/symbols-and-api.md` for complete API reference
2. **Try** a simple query: "Show me EUR/USD price"
3. **Build** a dashboard: "Create a price widget for 3 forex pairs"
4. **Customize** based on your needs

---

## File Structure

```
deriv-api-market-data/
├── SKILL.md                      ← Main skill definition
├── README.md                     ← Detailed guide
├── references/
│   ├── symbols-and-api.md       ← **START HERE for symbols & API**
│   └── artifact-templates.md    ← Widget code templates
└── scripts/
    └── deriv-api-helpers.js     ← Helper functions
```

## Example Commands to Try

```
1. "What's the current EUR/USD price?"
2. "Show me all available forex symbols"
3. "Create a price ticker widget for EUR/USD and GBP/USD"
4. "Get Bitcoin and Ethereum prices"
5. "Build a market dashboard showing 5 currency pairs"
6. "What contracts are available for Bitcoin?"
7. "Show me a chart of EUR/USD over time"
8. "List all cryptocurrency symbols you can get prices for"
```

## Support

Everything you need is in the skill files:
- **Questions about symbols?** → See `references/symbols-and-api.md`
- **Need code examples?** → See `references/symbols-and-api.md` or `artifact-templates.md`
- **Want to understand the API?** → Read the full `README.md`
- **Need helper functions?** → Check `scripts/deriv-api-helpers.js`

Happy trading! 📈
