/**
 * Deriv API Helper Functions
 * Simplify common market data queries
 */

const DERIV_API = 'https://api.deriv.com/api/v3/';

/**
 * Get all active symbols
 * @param {string} filter - 'forex', 'crypto', 'indices', 'stocks', or 'all'
 * @returns {Promise<Array>} Array of symbol objects
 */
async function getActiveSymbols(filter = 'all') {
  try {
    const response = await fetch(DERIV_API, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ active_symbols: 'brief' })
    });
    
    const data = await response.json();
    
    if (data.error) {
      console.error('API Error:', data.error.message);
      return [];
    }
    
    const symbols = data.active_symbols || [];
    
    // Filter by type if requested
    if (filter === 'forex') {
      return symbols.filter(s => s.symbol.startsWith('frx'));
    } else if (filter === 'crypto') {
      return symbols.filter(s => s.symbol.startsWith('cry'));
    } else if (filter === 'indices') {
      return symbols.filter(s => /^R_\d+$/.test(s.symbol));
    } else if (filter === 'stocks') {
      return symbols.filter(s => !s.symbol.startsWith('frx') && 
                                !s.symbol.startsWith('cry') && 
                                !/^R_\d+$/.test(s.symbol));
    }
    
    return symbols;
  } catch (error) {
    console.error('Failed to fetch symbols:', error);
    return [];
  }
}

/**
 * Get real-time price for a symbol
 * @param {string} symbol - e.g., 'frxEURUSD'
 * @returns {Promise<Object>} { symbol, bid, ask, time }
 */
async function getPrice(symbol) {
  try {
    const response = await fetch(DERIV_API, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        ticks: symbol,
        subscribe: 0  // 0 for single request, 1 for streaming
      })
    });
    
    const data = await response.json();
    
    if (data.error) {
      return { error: data.error.message, symbol };
    }
    
    if (data.tick) {
      return {
        symbol: data.tick.symbol,
        bid: data.tick.bid,
        ask: data.tick.ask,
        timestamp: data.tick.epoch,
        time: new Date(data.tick.epoch * 1000).toISOString()
      };
    }
    
    return { error: 'No tick data received', symbol };
  } catch (error) {
    return { error: error.message, symbol };
  }
}

/**
 * Get multiple prices at once
 * @param {Array<string>} symbols - Array of symbols
 * @returns {Promise<Object>} Map of symbol to price data
 */
async function getPrices(symbols) {
  const results = {};
  
  for (const symbol of symbols) {
    results[symbol] = await getPrice(symbol);
    // Small delay to avoid overwhelming the API
    await new Promise(r => setTimeout(r, 100));
  }
  
  return results;
}

/**
 * Get available contracts for a symbol
 * @param {string} symbol - e.g., 'frxEURUSD'
 * @returns {Promise<Object>} Contract data
 */
async function getContracts(symbol) {
  try {
    const response = await fetch(DERIV_API, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ contracts_for: symbol })
    });
    
    const data = await response.json();
    
    if (data.error) {
      return { error: data.error.message, symbol };
    }
    
    return {
      symbol: data.contracts_for,
      contracts: data.contracts_for,
      available: data.available || []
    };
  } catch (error) {
    return { error: error.message, symbol };
  }
}

/**
 * Get detailed asset information
 * @returns {Promise<Object>} Asset data
 */
async function getAssets() {
  try {
    const response = await fetch(DERIV_API, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ assets: 1 })
    });
    
    const data = await response.json();
    
    if (data.error) {
      return { error: data.error.message };
    }
    
    return data.assets || [];
  } catch (error) {
    return { error: error.message };
  }
}

/**
 * Test API connectivity
 * @returns {Promise<boolean>} True if API is responsive
 */
async function testConnection() {
  try {
    const response = await fetch(DERIV_API, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ping: 1 })
    });
    
    const data = await response.json();
    return !data.error;
  } catch (error) {
    return false;
  }
}

/**
 * Get historical ticks for a symbol
 * @param {string} symbol - e.g., 'frxEURUSD'
 * @param {number} count - Number of ticks to retrieve
 * @returns {Promise<Array>} Array of tick data
 */
async function getTicks(symbol, count = 10) {
  try {
    const response = await fetch(DERIV_API, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        ticks_history: symbol,
        adjust_start_time: 1,
        count: count,
        end: 'latest',
        start: 1,
        style: 'ticks'
      })
    });
    
    const data = await response.json();
    
    if (data.error) {
      return { error: data.error.message };
    }
    
    return {
      symbol: data.echo_req?.ticks_history,
      ticks: data.history?.times || [],
      prices: data.history?.close || []
    };
  } catch (error) {
    return { error: error.message };
  }
}

// Export for use in artifacts
if (typeof module !== 'undefined' && module.exports) {
  module.exports = {
    getActiveSymbols,
    getPrice,
    getPrices,
    getContracts,
    getAssets,
    testConnection,
    getTicks
  };
}
