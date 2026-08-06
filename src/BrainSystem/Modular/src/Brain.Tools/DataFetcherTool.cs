using System.Net.Http;
using System.Net.WebSockets;
using System.Text.Json;

namespace Brain.Tools;

/// <summary>
/// Data fetcher tool: retrieves market data from Deriv's public API.
/// Uses WebSocket connection to Deriv's API for real-time tick data.
/// </summary>
public sealed class DataFetcherTool : ITool, IDisposable
{
    private readonly HttpClient _httpClient;
    private const string DerivApiUrl = "https://api.deriv.com";

    public DataFetcherTool(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public string Name => "DataFetcher";
    public string Description => "Fetches market data (ticks, candles, volatility indices) from Deriv's public API.";
    public string ParameterSchema => "{\"symbol\": \"string\", \"count\": \"int\"}";

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("symbol", out var symbolObj) || symbolObj is not string symbol)
            return ToolResult.Fail("Missing required parameter: symbol");

        var count = parameters.TryGetValue("count", out var c) ? Convert.ToInt32(c) : 100;

        try
        {
            var requestUrl = DerivApiUrl + "/ticks_history?symbol=" + symbol + "&count=" + count + "&style=ticks";
            var response = await _httpClient.GetStringAsync(requestUrl);
            var doc = JsonDocument.Parse(response);

            if (doc.RootElement.TryGetProperty("error", out var error))
                return ToolResult.Fail("Deriv API error: " + error.GetString());

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Symbol: " + symbol);

            if (doc.RootElement.TryGetProperty("history", out var history))
            {
                if (history.TryGetProperty("prices", out var prices))
                {
                    var priceList = prices.EnumerateArray().Select(p => p.GetDouble()).ToArray();
                    sb.AppendLine("Prices: " + string.Join(", ", priceList.Take(10)) + (priceList.Length > 10 ? "..." : ""));
                    sb.AppendLine("Total ticks: " + priceList.Length);
                }
            }

            return ToolResult.Ok(sb.ToString());
        }
        catch (Exception ex)
        {
            return ToolResult.Fail("Data fetch failed: " + ex.Message);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
