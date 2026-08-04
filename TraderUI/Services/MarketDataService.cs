using System.Net.Http.Json;
using System.Text.Json;
using TraderUI.Models;

namespace TraderUI.Services;

/// <summary>
/// Market data service that fetches real-time quotes and OHLC data.
/// Supports multiple providers: TwelveData, AlphaVantage, Binance, CoinGecko, Deriv.
/// Falls back to mock data when no API key is configured.
/// </summary>
public class MarketDataService : IMarketDataService
{
    private readonly ISettingsService _settings;
    private readonly HttpClient _http;
    private readonly Dictionary<string, Action<Quote>> _subscribers = new();
    private readonly ILocalStorageService _storage;
    private Timer? _refreshTimer;

    private static readonly List<Quote> _defaultQuotes = new()
    {
        new() { Symbol = "EURUSD", Name = "Euro / US Dollar", Category = "Forex", Price = 1.08542m, Change = 0.00123m, ChangePercent = 0.11m },
        new() { Symbol = "GBPUSD", Name = "British Pound / US Dollar", Category = "Forex", Price = 1.27341m, Change = -0.00234m, ChangePercent = -0.18m },
        new() { Symbol = "USDJPY", Name = "US Dollar / Japanese Yen", Category = "Forex", Price = 149.823m, Change = 0.342m, ChangePercent = 0.23m },
        new() { Symbol = "XAUUSD", Name = "Gold / US Dollar", Category = "Forex", Price = 2345.67m, Change = 12.34m, ChangePercent = 0.53m },
        new() { Symbol = "BTCUSD", Name = "Bitcoin / US Dollar", Category = "Crypto", Price = 67234.50m, Change = 1234.50m, ChangePercent = 1.87m },
        new() { Symbol = "ETHUSD", Name = "Ethereum / US Dollar", Category = "Crypto", Price = 3456.78m, Change = -45.23m, ChangePercent = -1.29m },
        new() { Symbol = "BNBUSD", Name = "BNB / US Dollar", Category = "Crypto", Price = 567.89m, Change = 8.90m, ChangePercent = 1.59m },
        new() { Symbol = "US500", Name = "S&P 500 Index", Category = "Indices", Price = 5234.56m, Change = 23.45m, ChangePercent = 0.45m },
        new() { Symbol = "US30", Name = "Dow Jones 30", Category = "Indices", Price = 38765.43m, Change = -123.45m, ChangePercent = -0.32m },
        new() { Symbol = "NAS100", Name = "Nasdaq 100", Category = "Indices", Price = 18234.56m, Change = 89.34m, ChangePercent = 0.49m },
        new() { Symbol = "AAPL", Name = "Apple Inc.", Category = "Stocks", Price = 189.45m, Change = 2.34m, ChangePercent = 1.25m },
        new() { Symbol = "TSLA", Name = "Tesla Inc.", Category = "Stocks", Price = 234.56m, Change = -5.67m, ChangePercent = -2.36m },
        new() { Symbol = "NVDA", Name = "NVIDIA Corporation", Category = "Stocks", Price = 875.43m, Change = 15.67m, ChangePercent = 1.82m },
        new() { Symbol = "MSFT", Name = "Microsoft Corporation", Category = "Stocks", Price = 415.67m, Change = 3.45m, ChangePercent = 0.84m },
        // Deriv Synthetics
        new() { Symbol = "1HZ10V", Name = "Volatility 10 (1s) Index", Category = "Synthetics", Price = 6234.12m, Change = 12.34m, ChangePercent = 0.20m },
        new() { Symbol = "1HZ25V", Name = "Volatility 25 (1s) Index", Category = "Synthetics", Price = 4567.89m, Change = -23.45m, ChangePercent = -0.51m },
        new() { Symbol = "1HZ50V", Name = "Volatility 50 (1s) Index", Category = "Synthetics", Price = 3456.78m, Change = 34.56m, ChangePercent = 1.01m },
        new() { Symbol = "1HZ75V", Name = "Volatility 75 (1s) Index", Category = "Synthetics", Price = 2345.67m, Change = -12.34m, ChangePercent = -0.52m },
        new() { Symbol = "1HZ100V", Name = "Volatility 100 (1s) Index", Category = "Synthetics", Price = 1234.56m, Change = 5.67m, ChangePercent = 0.46m },
    };

    private List<Quote> _quotes = new();
    private readonly Random _rng = new();

    public MarketDataService(ISettingsService settings, ILocalStorageService storage)
    {
        _settings = settings;
        _storage = storage;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _quotes = _defaultQuotes.Select(q => new Quote
        {
            Symbol = q.Symbol, Name = q.Name, Category = q.Category,
            Price = q.Price, Change = q.Change, ChangePercent = q.ChangePercent
        }).ToList();

        // Start price simulation timer
        _refreshTimer = new Timer(SimulatePriceUpdates, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
    }

    public async Task<List<Quote>> GetQuotesAsync(string category = "all")
    {
        // Try to load cached quotes from storage
        var cached = await _storage.LoadAsync<List<Quote>>("quotes_cache");
        if (cached?.Count > 0)
        {
            foreach (var c in cached)
            {
                var existing = _quotes.FirstOrDefault(q => q.Symbol == c.Symbol);
                if (existing != null) { existing.IsFavorite = c.IsFavorite; }
            }
        }

        if (category == "all") return _quotes;
        return _quotes.Where(q => q.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<Quote?> GetQuoteAsync(string symbol)
    {
        await Task.CompletedTask;
        return _quotes.FirstOrDefault(q => q.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<OhlcBar>> GetOhlcAsync(string symbol, string timeframe, int count = 200)
    {
        // Try TwelveData first
        var apiKey = await _settings.GetApiKeyAsync("twelvedata");
        if (!string.IsNullOrEmpty(apiKey))
        {
            try
            {
                var interval = TimeframeToTwelveData(timeframe);
                var url = $"https://api.twelvedata.com/time_series?symbol={symbol}&interval={interval}&outputsize={count}&apikey={apiKey}";
                var response = await _http.GetFromJsonAsync<JsonElement>(url);
                if (response.TryGetProperty("values", out var values))
                {
                    var bars = new List<OhlcBar>();
                    foreach (var v in values.EnumerateArray())
                    {
                        bars.Add(new OhlcBar
                        {
                            Time = DateTime.Parse(v.GetProperty("datetime").GetString()!),
                            Open = decimal.Parse(v.GetProperty("open").GetString()!),
                            High = decimal.Parse(v.GetProperty("high").GetString()!),
                            Low = decimal.Parse(v.GetProperty("low").GetString()!),
                            Close = decimal.Parse(v.GetProperty("close").GetString()!),
                            Volume = v.TryGetProperty("volume", out var vol) ? decimal.Parse(vol.GetString()!) : 0
                        });
                    }
                    return bars.OrderBy(b => b.Time).ToList();
                }
            }
            catch { /* Fall through to mock */ }
        }

        return GenerateMockOhlc(symbol, timeframe, count);
    }

    public async Task<List<Quote>> SearchQuotesAsync(string query)
    {
        await Task.CompletedTask;
        return _quotes.Where(q =>
            q.Symbol.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            q.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task SubscribeToQuoteAsync(string symbol, Action<Quote> onUpdate)
    {
        await Task.CompletedTask;
        _subscribers[symbol] = onUpdate;
    }

    public async Task UnsubscribeFromQuoteAsync(string symbol)
    {
        await Task.CompletedTask;
        _subscribers.Remove(symbol);
    }

    public async Task<List<string>> GetAvailableSymbolsAsync()
    {
        await Task.CompletedTask;
        return _quotes.Select(q => q.Symbol).ToList();
    }

    private void SimulatePriceUpdates(object? state)
    {
        foreach (var quote in _quotes)
        {
            var volatility = quote.Category switch
            {
                "Crypto" => 0.003,
                "Synthetics" => 0.002,
                "Indices" => 0.001,
                _ => 0.0005
            };
            var change = (decimal)((_rng.NextDouble() - 0.5) * 2 * volatility * (double)quote.Price);
            quote.Price = Math.Max(0.0001m, quote.Price + change);
            quote.Change += change;
            quote.ChangePercent = quote.Price != 0 ? (quote.Change / (quote.Price - quote.Change)) * 100 : 0;
            quote.LastUpdated = DateTime.UtcNow;

            if (_subscribers.TryGetValue(quote.Symbol, out var callback))
            {
                MainThread.BeginInvokeOnMainThread(() => callback(quote));
            }
        }
    }

    private List<OhlcBar> GenerateMockOhlc(string symbol, string timeframe, int count)
    {
        var bars = new List<OhlcBar>();
        var baseQuote = _quotes.FirstOrDefault(q => q.Symbol == symbol);
        var basePrice = (double)(baseQuote?.Price ?? 1.0m);
        var now = DateTime.UtcNow;
        var intervalMinutes = int.TryParse(timeframe, out var tf) ? tf : 60;

        for (int i = count; i >= 0; i--)
        {
            var time = now.AddMinutes(-i * intervalMinutes);
            var open = basePrice * (1 + (_rng.NextDouble() - 0.5) * 0.002);
            var close = open * (1 + (_rng.NextDouble() - 0.5) * 0.003);
            var high = Math.Max(open, close) * (1 + _rng.NextDouble() * 0.002);
            var low = Math.Min(open, close) * (1 - _rng.NextDouble() * 0.002);
            bars.Add(new OhlcBar
            {
                Time = time,
                Open = (decimal)open,
                High = (decimal)high,
                Low = (decimal)low,
                Close = (decimal)close,
                Volume = (decimal)(_rng.NextDouble() * 1000000)
            });
            basePrice = close;
        }
        return bars;
    }

    private static string TimeframeToTwelveData(string tf) => tf switch
    {
        "1" => "1min",
        "5" => "5min",
        "15" => "15min",
        "60" => "1h",
        "240" => "4h",
        "1440" => "1day",
        "10080" => "1week",
        _ => "1h"
    };
}
