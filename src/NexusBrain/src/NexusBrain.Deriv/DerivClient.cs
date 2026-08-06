using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using NexusBrain.Core;

namespace NexusBrain.Deriv;

/// <summary>
/// Client for the Deriv public WebSocket API (no API keys required — public data).
/// Provides real-time ticks, OHLC candles, and active symbol lists for the
/// Volatility Index, Drift Switch Index, forex pairs and other instruments.
/// </summary>
public sealed class DerivClient : IAsyncDisposable
{
    private readonly Uri _uri;
    private ClientWebSocket _ws = new();
    private int _reqId;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly Dictionary<int, TaskCompletionSource<JsonElement>> _pending = new();
    private readonly object _sync = new();
    private CancellationTokenSource _readerCts = new();
    private Task? _readerTask;

    public DerivClient(string wsUrl)
        => _uri = new Uri(wsUrl);

    /// <summary>Synthetic Volatility Index symbols (Deriv).</summary>
    public static readonly string[] VolatilityIndexSymbols =
    {
        "R_10", "R_25", "R_50", "R_75", "R_100",
        "1HZ10V", "1HZ25V", "1HZ50V", "1HZ75V", "1HZ100V"
    };

    /// <summary>Drift Switch Index symbols (Deriv).</summary>
    public static readonly string[] DriftSwitchIndexSymbols =
    {
        "1HZ150V", "1HZ250V", "1HZ500V", "1HZ1000V"
    };

    /// <summary>Common forex pairs available on Deriv.</summary>
    public static readonly string[] ForexSymbols =
    {
        "frxEURUSD", "frxGBPUSD", "frxUSDJPY", "frxUSDCHF",
        "frxAUDUSD", "frxUSDCAD", "frxNZDUSD", "frxEURGBP"
    };

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (_ws.State == WebSocketState.Open) return;
        _ws = new ClientWebSocket();
        _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        await _ws.ConnectAsync(_uri, ct).ConfigureAwait(false);
        _readerCts = new CancellationTokenSource();
        _readerTask = Task.Run(() => ReaderLoop(_readerCts.Token));
    }

    private async Task ReaderLoop(CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];
        var sb = new StringBuilder();
        while (!ct.IsCancellationRequested && _ws.State == WebSocketState.Open)
        {
            sb.Clear();
            WebSocketReceiveResult result;
            do
            {
                try { result = await _ws.ReceiveAsync(buffer, ct).ConfigureAwait(false); }
                catch { return; }
                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            } while (!result.EndOfMessage);
            if (result.MessageType == WebSocketMessageType.Close) return;
            var text = sb.ToString();
            try
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement.Clone();
                if (root.TryGetProperty("req_id", out var ridEl) && ridEl.TryGetInt32(out var rid))
                {
                    lock (_sync)
                    {
                        if (_pending.TryGetValue(rid, out var tcs))
                        {
                            _pending.Remove(rid);
                            tcs.TrySetResult(root);
                        }
                    }
                }
            }
            catch { /* ignore malformed frames */ }
        }
    }

    private async Task<JsonElement> RequestAsync(Dictionary<string, object> payload, CancellationToken ct)
    {
        await ConnectAsync(ct).ConfigureAwait(false);
        var rid = Interlocked.Increment(ref _reqId);
        payload["req_id"] = rid;
        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_sync) _pending[rid] = tcs;
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _sendLock.WaitAsync(ct).ConfigureAwait(false);
        try { await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct).ConfigureAwait(false); }
        finally { _sendLock.Release(); }
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linked.CancelAfter(TimeSpan.FromSeconds(20));
        await using var _ = linked.Token.Register(() => tcs.TrySetCanceled());
        return await tcs.Task.ConfigureAwait(false);
    }

    /// <summary>Ping/pong liveness check.</summary>
    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await RequestAsync(new Dictionary<string, object> { ["ping"] = 1 }, ct).ConfigureAwait(false);
            return res.TryGetProperty("ping", out _) || res.TryGetProperty("pong", out _);
        }
        catch { return false; }
    }

    /// <summary>Latest quote for a symbol as a tick.</summary>
    public async Task<Tick?> GetTickAsync(string symbol, CancellationToken ct = default)
    {
        try
        {
            var res = await RequestAsync(new Dictionary<string, object>
            {
                ["ticks_history"] = symbol, ["count"] = 1, ["end"] = "latest", ["style"] = "ticks"
            }, ct).ConfigureAwait(false);
            if (res.TryGetProperty("history", out var hist) &&
                hist.TryGetProperty("prices", out var prices) &&
                prices.GetArrayLength() > 0)
            {
                long epoch = 0;
                if (hist.TryGetProperty("times", out var times) && times.GetArrayLength() > 0)
                    epoch = times[times.GetArrayLength() - 1].GetInt64();
                return new Tick(epoch, prices[prices.GetArrayLength() - 1].GetDouble());
            }
        }
        catch { /* network error */ }
        return null;
    }

    /// <summary>Fetch OHLC candles for a symbol.</summary>
    public async Task<List<Candle>> GetCandlesAsync(string symbol, int granularitySec, int count, CancellationToken ct = default)
    {
        try
        {
            var res = await RequestAsync(new Dictionary<string, object>
            {
                ["ticks_history"] = symbol, ["count"] = count, ["end"] = "latest",
                ["style"] = "candles", ["granularity"] = granularitySec
            }, ct).ConfigureAwait(false);
            var list = new List<Candle>();
            if (res.TryGetProperty("candles", out var arr))
            {
                foreach (var e in arr.EnumerateArray())
                {
                    list.Add(new Candle(
                        e.GetProperty("epoch").GetInt64(),
                        e.GetProperty("open").GetDouble(),
                        e.GetProperty("high").GetDouble(),
                        e.GetProperty("low").GetDouble(),
                        e.GetProperty("close").GetDouble(),
                        e.TryGetProperty("volume", out var vol) ? vol.GetDouble() : 0));
                }
            }
            return list;
        }
        catch { return new List<Candle>(); }
    }

    /// <summary>List active symbols (optionally filtered by market).</summary>
    public async Task<List<string>> GetActiveSymbolsAsync(string? market = null, CancellationToken ct = default)
    {
        try
        {
            var res = await RequestAsync(new Dictionary<string, object>
            {
                ["active_symbols"] = "brief", ["product_type"] = "basic"
            }, ct).ConfigureAwait(false);
            var symbols = new List<string>();
            if (res.TryGetProperty("active_symbols", out var arr))
            {
                foreach (var e in arr.EnumerateArray())
                {
                    if (market is null ||
                        (e.TryGetProperty("market", out var m) && m.GetString() == market))
                    {
                        if (e.TryGetProperty("symbol", out var s))
                            symbols.Add(s.GetString()!);
                    }
                }
            }
            return symbols;
        }
        catch { return new List<string>(); }
    }

    /// <summary>
    /// Stream live ticks for a symbol, invoking the callback for each tick.
    /// </summary>
    public async Task StreamTicksAsync(string symbol, Action<Tick> onTick, CancellationToken ct = default)
    {
        await ConnectAsync(ct).ConfigureAwait(false);
        var payload = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["ticks"] = symbol, ["subscribe"] = 1
        });
        var bytes = Encoding.UTF8.GetBytes(payload);
        await _sendLock.WaitAsync(ct).ConfigureAwait(false);
        try { await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct).ConfigureAwait(false); }
        finally { _sendLock.Release(); }

        var buffer = new byte[64 * 1024];
        var sb = new StringBuilder();
        while (!ct.IsCancellationRequested && _ws.State == WebSocketState.Open)
        {
            sb.Clear();
            WebSocketReceiveResult result;
            try { result = await _ws.ReceiveAsync(buffer, ct).ConfigureAwait(false); }
            catch { break; }
            sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            if (!result.EndOfMessage) continue;
            if (result.MessageType == WebSocketMessageType.Close) break;
            try
            {
                using var doc = JsonDocument.Parse(sb.ToString());
                var root = doc.RootElement;
                if (root.TryGetProperty("tick", out var tick))
                {
                    double quote = tick.TryGetProperty("quote", out var q) ? q.GetDouble() : 0;
                    long epoch = tick.TryGetProperty("epoch", out var ep) ? ep.GetInt64() : 0;
                    onTick(new Tick(epoch, quote));
                }
            }
            catch { /* ignore */ }
        }
    }

    public async ValueTask DisposeAsync()
    {
        try { _readerCts.Cancel(); } catch { }
        if (_readerTask is not null) { try { await _readerTask.ConfigureAwait(false); } catch { } }
        try { if (_ws.State == WebSocketState.Open) await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None).ConfigureAwait(false); } catch { }
        _ws.Dispose();
    }
}
