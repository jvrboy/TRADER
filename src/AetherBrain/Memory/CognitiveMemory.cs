using System.Collections.Concurrent;

namespace AetherBrain.Memory;

public enum MemoryLayer { Working, Episodic, Semantic }

public sealed record MemoryRecord(
    Guid Id,
    string Content,
    float[] Embedding,
    MemoryLayer Layer,
    double Importance,
    DateTimeOffset CreatedAt,
    int AccessCount = 0);

public sealed class CognitiveMemory
{
    private readonly ConcurrentDictionary<Guid, MemoryRecord> _records = new();
    private readonly int _workingCapacity;

    public CognitiveMemory(int workingCapacity = 64) => _workingCapacity = Math.Max(8, workingCapacity);

    public MemoryRecord Remember(string content, MemoryLayer layer, double importance = 0.5)
    {
        var record = new MemoryRecord(
            Guid.NewGuid(), content, Embed(content), layer, Math.Clamp(importance, 0, 1), DateTimeOffset.UtcNow);
        _records[record.Id] = record;
        TrimWorkingMemory();
        return record;
    }

    public IReadOnlyList<MemoryRecord> Recall(string query, int limit = 5)
    {
        var queryVector = Embed(query);
        return _records.Values
            .Select(record => new { Record = record, Score = Cosine(queryVector, record.Embedding) * .75 + record.Importance * .25 })
            .OrderByDescending(item => item.Score)
            .Take(Math.Max(1, limit))
            .Select(item =>
            {
                var updated = item.Record with { AccessCount = item.Record.AccessCount + 1 };
                _records[updated.Id] = updated;
                return updated;
            })
            .ToArray();
    }

    public void Consolidate()
    {
        foreach (var record in _records.Values.Where(record => record.Layer == MemoryLayer.Episodic && record.Importance >= .72))
        {
            _records[record.Id] = record with { Layer = MemoryLayer.Semantic, Importance = Math.Min(1, record.Importance + .08) };
        }
    }

    public IReadOnlyCollection<MemoryRecord> Snapshot() => _records.Values.ToArray();

    private void TrimWorkingMemory()
    {
        var overflow = _records.Values
            .Where(record => record.Layer == MemoryLayer.Working)
            .OrderBy(record => record.Importance)
            .ThenBy(record => record.CreatedAt)
            .Skip(_workingCapacity)
            .ToArray();
        foreach (var record in overflow) _records.TryRemove(record.Id, out _);
    }

    private static float[] Embed(string text)
    {
        const int dimensions = 48;
        var vector = new float[dimensions];
        foreach (var token in text.ToLowerInvariant().Split([' ', ',', '.', ':', ';', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var hash = StringComparer.Ordinal.GetHashCode(token);
            vector[Math.Abs(hash % dimensions)] += hash % 2 == 0 ? 1f : -1f;
        }
        return vector;
    }

    private static double Cosine(float[] left, float[] right)
    {
        double dot = 0, leftMagnitude = 0, rightMagnitude = 0;
        for (var index = 0; index < left.Length; index++)
        {
            dot += left[index] * right[index];
            leftMagnitude += left[index] * left[index];
            rightMagnitude += right[index] * right[index];
        }
        return leftMagnitude == 0 || rightMagnitude == 0 ? 0 : dot / Math.Sqrt(leftMagnitude * rightMagnitude);
    }
}
