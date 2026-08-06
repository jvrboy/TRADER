using System.Collections.Concurrent;

namespace Brain.Memory;

/// <summary>
/// Long-Term Memory: vector database with approximate nearest neighbor search.
/// Uses a brute-force cosine similarity search (suitable for moderate-size stores).
/// Can be upgraded to HNSW for larger datasets.
/// </summary>
public sealed class LongTermMemory
{
    private readonly ConcurrentDictionary<Guid, MemoryEntry> _store = new();
    private readonly int _maxSize;
    private readonly object _pruneLock = new();

    public LongTermMemory(int maxSize = 10000)
    {
        _maxSize = maxSize;
    }

    public void Store(MemoryEntry entry)
    {
        if (entry.Embedding == null) return;
        _store[entry.Id] = entry;
        Prune();
    }

    /// <summary>
    /// Retrieves the top-k most similar memories using cosine similarity.
    /// </summary>
    public IReadOnlyList<MemoryEntry> Query(float[] queryEmbedding, int topK = 5)
    {
        var results = new List<(MemoryEntry entry, float similarity)>();

        foreach (var entry in _store.Values)
        {
            if (entry.Embedding == null) continue;
            var sim = CosineSimilarity(queryEmbedding, entry.Embedding);
            results.Add((entry, sim));
        }

        return results
            .OrderByDescending(r => r.similarity)
            .Take(topK)
            .Select(r => r.entry)
            .ToArray();
    }

    /// <summary>
    /// Retrieves memories by text content (simple substring match).
    /// </summary>
    public IReadOnlyList<MemoryEntry> QueryByText(string text, int topK = 5)
    {
        return _store.Values
            .Where(e => e.Content.Contains(text, StringComparison.OrdinalIgnoreCase))
            .Take(topK)
            .ToArray();
    }

    public int Count => _store.Count;

    public void Remove(Guid id) => _store.TryRemove(id, out _);

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;
        var dot = 0f;
        var magA = 0f;
        var magB = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        var denom = MathF.Sqrt(magA) * MathF.Sqrt(magB);
        return denom == 0 ? 0 : dot / denom;
    }

    private void Prune()
    {
        if (_store.Count <= _maxSize) return;
        lock (_pruneLock)
        {
            if (_store.Count <= _maxSize) return;
            var toRemove = _store.Values
                .OrderBy(e => e.RelevanceScore)
                .Take(_store.Count - _maxSize)
                .Select(e => e.Id)
                .ToArray();
            foreach (var id in toRemove)
                _store.TryRemove(id, out _);
        }
    }
}
