using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using DsiAgentic.Core;

namespace DsiAgentic.Deriv;

/// <summary>
/// Minimal Deriv WebSocket client for public data (ticks_history, ticks, active_symbols).
/// Uses a single ClientWebSocket; requests are correlated via req_id.
/// </summary>
public sealed class DerivClient : IAsyncDisposable
{
    private readonly Uri _uri;
    private ClientWebSocket _ws = new();
    private int _reqId = 0;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly Dictionary<int, TaskCompletionSource<JsonElement>> _pending = new();
    private CancellationTokenSource _readerCts = new();
    private Task? _readerTask;

    public DerivClient(string wsUrl)
    {
        _uri = new Uri(wsUrl);
    }

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
                    lock (_pending)
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
        lock (_pending) _pending[rid] = tcs;
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _sendLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
        }
        finally { _sendLock.Release(); }
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linked.CancelAfter(TimeSpan.FromSeconds(15));
        await using var _ = linked.Token.Register(() => tcs.TrySetCanceled());
        return await tcs.Task.ConfigureAwait(false);
    }

    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await RequestAsync(new Dictionary<string, object> { ["ping"] = 1 }, ct).ConfigureAwait(false);
            return res.TryGetProperty("ping", out _) || res.TryGetProperty("pong", out _);
        }
        catch { return false; }
    }

    public async Task<double?> GetQuoteAsync(string symbol, CancellationToken ct = default)
    {
        var res = await RequestAsync(new Dictionary<string, object>
        {
            ["ticks_history"] = symbol,
            ["count"] = 1,
            ["end"] = "latest",
            ["style"] = "ticks"
        }, ct).ConfigureAwait(false);
        if (res.TryGetProperty("history", out var hist) &&
            hist.TryGetProperty("prices", out var prices) &&
            prices.GetArrayLength() > 0)
            return prices[prices.GetArrayLength() - 1].GetDouble();
        return null;
    }

    public async Task<List<Candle>> GetCandlesAsync(string symbol, int granularitySec, int count, CancellationToken ct = default)
    {
        var res = await RequestAsync(new Dictionary<string, object>
        {
            ["ticks_history"] = symbol,
            ["count"] = count,
            ["end"] = "latest",
            ["style"] = "candles",
            ["granularity"] = granularitySec
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
                    0.0));
            }
        }
        return list;
    }

    public async ValueTask DisposeAsync()
    {
        try { _readerCts.Cancel(); } catch { }
        if (_readerTask is not null) { try { await _readerTask.ConfigureAwait(false); } catch { } }
        try { if (_ws.State == WebSocketState.Open) await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None).ConfigureAwait(false); } catch { }
        _ws.Dispose();
    }
}
