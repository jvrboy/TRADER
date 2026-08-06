using System.Net.Http;
using System.Text.Json;

namespace Brain.Training;

/// <summary>
/// Client for Deriv's public API. Fetches historical tick data for drift switch indices.
/// Falls back to synthetic data generation if the API is unavailable.
/// </summary>
public sealed class DerivApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.deriv.com";

    public DerivApiClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Fetches historical tick data for a given symbol.
    /// </summary>
    public async Task<List<TickData>> GetTicksAsync(string symbol, int count = 1000)
    {
        try
        {
            var url = BaseUrl + "/ticks_history?symbol=" + symbol + "&count=" + count + "&style=ticks";
            var response = await _httpClient.GetStringAsync(url);
            var doc = JsonDocument.Parse(response);

            if (doc.RootElement.TryGetProperty("error", out var error))
                throw new Exception("Deriv API error: " + error.GetString());

            var ticks = new List<TickData>();
            if (doc.RootElement.TryGetProperty("history", out var history))
            {
                var prices = history.GetProperty("prices").EnumerateArray().Select(p => p.GetDouble()).ToArray();
                var times = history.GetProperty("times").EnumerateArray().Select(t => t.GetInt64()).ToArray();

                for (int i = 0; i < prices.Length; i++)
                {
                    ticks.Add(new TickData
                    {
                        Symbol = symbol,
                        Price = prices[i],
                        Timestamp = DateTimeOffset.FromUnixTimeSeconds(times[i]).DateTime
                    });
                }
            }

            return ticks;
        }
        catch
        {
            return GenerateSyntheticData(symbol, count);
        }
    }

    /// <summary>
    /// Generates synthetic tick data that mimics drift behaviour.
    /// Used as fallback when the Deriv API is unavailable.
    /// </summary>
    public static List<TickData> GenerateSyntheticData(string symbol, int count)
    {
        var rng = new Random(42);
        var ticks = new List<TickData>();
        var price = 100.0;
        var baseTime = DateTime.UtcNow.AddSeconds(-count);

        for (int i = 0; i < count; i++)
        {
            var drift = (rng.NextDouble() - 0.5) * 0.002;
            var volatility = rng.NextDouble() * 0.001;
            price *= (1 + drift + volatility);
            ticks.Add(new TickData
            {
                Symbol = symbol,
                Price = price,
                Timestamp = baseTime.AddSeconds(i)
            });
        }

        return ticks;
    }

    public void Dispose() => _httpClient?.Dispose();
}

public sealed class TickData
{
    public string Symbol { get; set; } = string.Empty;
    public double Price { get; set; }
    public DateTime Timestamp { get; set; }
}
