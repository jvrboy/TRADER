using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TraderUI.Models;

namespace TraderUI.Services;

/// <summary>
/// Production Deriv Public WebSocket Client.
/// Connects to Deriv's default public endpoint (wss://ws.derivws.com/websockets/v3?app_id=1089)
/// with zero authentication required to stream real-time ticks, active symbols, and historical OHLC candles.
/// </summary>
public sealed class DerivWebSocketClient : IAsyncDisposable
{
    private const string DefaultWsUrl = "wss://ws.derivws.com/websockets/v3?app_id=1089";
    private readonly Uri _uri;
    private ClientWebSocket _ws = new();
    private int _reqId = 0;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly Dictionary<int, TaskCompletionSource<JsonElement>> _pendingRequests = new();
    private readonly Dictionary<string, Action<string, decimal, DateTime>> _tickSubscriptions = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource _cts = new();
    private Task? _listenerTask;
    private bool _isConnecting;

    public bool IsConnected => _ws.State == WebSocketState.Open;

    public DerivWebSocketClient(string? wsUrl = null)
    {
        _uri = new Uri(string.IsNullOrWhiteSpace(wsUrl) ? DefaultWsUrl : wsUrl);
    }

    public async Task EnsureConnectedAsync(CancellationToken ct = default)
    {
        if (IsConnected) return;

        await _sendLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (IsConnected || _isConnecting) return;
            _isConnecting = true;

            _ws?.Dispose();
            _ws = new ClientWebSocket();
            _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);

            using var connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, connectCts.Token);
            await _ws.ConnectAsync(_uri, linked.Token).ConfigureAwait(false);

            _cts = new CancellationTokenSource();
            _listenerTask = Task.Run(() => ListenerLoopAsync(_cts.Token));

            // Resubscribe to existing tick streams
            foreach (var symbol in _tickSubscriptions.Keys)
            {
                _ = SendAsync(new Dictionary<string, object>
                {
                    ["ticks"] = symbol,
                    ["subscribe"] = 1
                }, CancellationToken.None);
            }
        }
        catch
        {
            // Connection failed or offline
        }
        finally
        {
            _isConnecting = false;
            _sendLock.Release();
        }
    }

    public async Task<List<Quote>> FetchAllActiveSymbolsAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await SendRequestAsync(new Dictionary<string, object>
            {
                ["active_symbols"] = "full"
            }, ct).ConfigureAwait(false);

            var quotes = new List<Quote>();
            if (res.TryGetProperty("active_symbols", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    var symbol = item.GetProperty("symbol").GetString() ?? "";
                    var displayName = item.GetProperty("display_name").GetString() ?? symbol;
                    var market = item.GetProperty("market").GetString() ?? "";
                    var submarket = item.GetProperty("submarket_display_name").GetString() ?? "";
                    var spot = item.TryGetProperty("spot", out var sp) ? sp.GetDecimal() : 1.0m;

                    var category = market.ToLowerInvariant() switch
                    {
                        "synthetic_index" => "Synthetics",
                        "forex" => "Forex",
                        "cryptocurrency" => "Crypto",
                        "indices" => "Indices",
                        "commodities" => "Commodities",
                        _ => submarket.Contains("Forex") ? "Forex" :
                             submarket.Contains("Crypto") ? "Crypto" :
                             submarket.Contains("Volatility") ? "Synthetics" : "Synthetics"
                    };

                    quotes.Add(new Quote
                    {
                        Symbol = symbol,
                        Name = displayName,
                        Category = category,
                        Price = spot,
                        Change = 0m,
                        ChangePercent = 0m,
                        LastUpdated = DateTime.UtcNow
                    });
                }
            }
            return quotes;
        }
        catch
        {
            return new List<Quote>();
        }
    }

    public async Task SubscribeTickAsync(string symbol, Action<string, decimal, DateTime> onTick)
    {
        _tickSubscriptions[symbol] = onTick;
        try
        {
            await EnsureConnectedAsync().ConfigureAwait(false);
            if (IsConnected)
            {
                await SendAsync(new Dictionary<string, object>
                {
                    ["ticks"] = symbol,
                    ["subscribe"] = 1
                }, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch { }
    }

    public async Task UnsubscribeTickAsync(string symbol)
    {
        _tickSubscriptions.Remove(symbol);
        try
        {
            if (IsConnected)
            {
                await SendAsync(new Dictionary<string, object>
                {
                    ["forget"] = symbol
                }, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch { }
    }

    public async Task<List<OhlcBar>> FetchCandlesAsync(string symbol, int granularitySec, int count = 200, CancellationToken ct = default)
    {
        try
        {
            var res = await SendRequestAsync(new Dictionary<string, object>
            {
                ["ticks_history"] = symbol,
                ["style"] = "candles",
                ["granularity"] = granularitySec,
                ["count"] = count,
                ["end"] = "latest"
            }, ct).ConfigureAwait(false);

            var bars = new List<OhlcBar>();
            if (res.TryGetProperty("candles", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in arr.EnumerateArray())
                {
                    var epoch = c.GetProperty("epoch").GetInt64();
                    var open = c.GetProperty("open").GetDecimal();
                    var high = c.GetProperty("high").GetDecimal();
                    var low = c.GetProperty("low").GetDecimal();
                    var close = c.GetProperty("close").GetDecimal();

                    bars.Add(new OhlcBar
                    {
                        Time = DateTimeOffset.FromUnixTimeSeconds(epoch).UtcDateTime,
                        Open = open,
                        High = high,
                        Low = low,
                        Close = close,
                        Volume = 1000m
                    });
                }
            }
            return bars;
        }
        catch
        {
            return new List<OhlcBar>();
        }
    }

    private async Task<JsonElement> SendRequestAsync(Dictionary<string, object> payload, CancellationToken ct)
    {
        await EnsureConnectedAsync(ct).ConfigureAwait(false);
        var reqId = Interlocked.Increment(ref _reqId);
        payload["req_id"] = reqId;

        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_pendingRequests)
        {
            _pendingRequests[reqId] = tcs;
        }

        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);

        await _sendLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        await using var reg = linked.Token.Register(() => tcs.TrySetCanceled());

        return await tcs.Task.ConfigureAwait(false);
    }

    private async Task SendAsync(Dictionary<string, object> payload, CancellationToken ct)
    {
        var reqId = Interlocked.Increment(ref _reqId);
        payload["req_id"] = reqId;
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);

        await _sendLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_ws.State == WebSocketState.Open)
            {
                await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
            }
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task ListenerLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[128 * 1024];
        var sb = new StringBuilder();

        while (!ct.IsCancellationRequested && _ws.State == WebSocketState.Open)
        {
            sb.Clear();
            WebSocketReceiveResult result;
            do
            {
                try
                {
                    result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);
                }
                catch
                {
                    return;
                }

                if (result.MessageType == WebSocketMessageType.Close) return;
                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            } while (!result.EndOfMessage);

            var message = sb.ToString();
            try
            {
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement.Clone();

                // 1. Correlate request response via req_id
                if (root.TryGetProperty("req_id", out var ridEl) && ridEl.TryGetInt32(out var rid))
                {
                    lock (_pendingRequests)
                    {
                        if (_pendingRequests.TryGetValue(rid, out var tcs))
                        {
                            _pendingRequests.Remove(rid);
                            tcs.TrySetResult(root);
                        }
                    }
                }

                // 2. Dispatch real-time tick updates
                if (root.TryGetProperty("tick", out var tickEl))
                {
                    var symbol = tickEl.GetProperty("symbol").GetString() ?? "";
                    var quotePrice = tickEl.GetProperty("quote").GetDecimal();
                    var epoch = tickEl.GetProperty("epoch").GetInt64();
                    var time = DateTimeOffset.FromUnixTimeSeconds(epoch).UtcDateTime;

                    if (_tickSubscriptions.TryGetValue(symbol, out var callback))
                    {
                        callback(symbol, quotePrice, time);
                    }
                }
            }
            catch
            {
                // Ignore frame parse anomalies
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        try { _cts.Cancel(); } catch { }
        try
        {
            if (_ws.State == WebSocketState.Open)
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch { }
        _ws.Dispose();
        _cts.Dispose();
    }
}
