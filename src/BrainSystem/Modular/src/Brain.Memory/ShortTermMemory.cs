using System.Collections.Concurrent;

namespace Brain.Memory;

/// <summary>
/// Short-Term Memory: circular buffer of the last 1000 interactions.
/// Thread-safe, non-blocking reads and writes.
/// </summary>
public sealed class ShortTermMemory
{
    private readonly ConcurrentQueue<MemoryEntry> _buffer = new();
    private readonly int _maxSize;
    private readonly object _pruneLock = new();

    public ShortTermMemory(int maxSize = 1000)
    {
        _maxSize = maxSize;
    }

    public void Add(MemoryEntry entry)
    {
        _buffer.Enqueue(entry);
        Prune();
    }

    public IReadOnlyList<MemoryEntry> GetAll()
    {
        return _buffer.ToArray();
    }

    public IReadOnlyList<MemoryEntry> GetRecent(int count)
    {
        return _buffer.ToArray().TakeLast(count).ToArray();
    }

    public IEnumerable<MemoryEntry> GetHighRelevance(float threshold = 0.7f)
    {
        return _buffer.Where(e => e.RelevanceScore >= threshold);
    }

    public void Clear()
    {
        while (_buffer.TryDequeue(out _)) { }
    }

    public int Count => _buffer.Count;

    private void Prune()
    {
        if (_buffer.Count <= _maxSize) return;
        lock (_pruneLock)
        {
            while (_buffer.Count > _maxSize && _buffer.TryDequeue(out _)) { }
        }
    }
}
