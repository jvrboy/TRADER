using System.Collections.Concurrent;

namespace DsiAgentic.Orchestrator;

/// <summary>Lightweight in-process pub/sub for agent events.</summary>
public sealed class MessageBus
{
    private readonly ConcurrentDictionary<string, List<Action<string, object>>> _subs = new();
    private readonly ConcurrentQueue<(string topic, object payload)> _history = new();
    private const int MaxHistory = 500;

    public void Subscribe(string topic, Action<string, object> handler)
        => _subs.GetOrAdd(topic, _ => new()).Add(handler);

    public void Publish(string topic, object payload)
    {
        _history.Enqueue((topic, payload));
        while (_history.Count > MaxHistory) _history.TryDequeue(out _);
        if (_subs.TryGetValue(topic, out var subs))
            foreach (var h in subs) { try { h(topic, payload); } catch { } }
        if (_subs.TryGetValue("*", out var wild))
            foreach (var h in wild) { try { h(topic, payload); } catch { } }
    }

    public IEnumerable<(string topic, object payload)> History(int limit = 100)
        => _history.Reverse().Take(limit);
}
