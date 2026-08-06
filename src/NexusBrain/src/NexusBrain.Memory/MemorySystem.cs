using System.Text.Json;

namespace NexusBrain.Memory;

/// <summary>A single episodic memory: a timestamped event with a feature signature.</summary>
public sealed class EpisodicMemory
{
    public long Id { get; set; }
    public long Epoch { get; set; }
    public required string Symbol { get; init; }
    public required string Event { get; init; }       // e.g. "DIVERGENCE_BULLISH"
    public double[]? Signature { get; init; }          // feature vector for similarity recall
    public double Outcome { get; set; }                // realised reward/result
    public string? Note { get; init; }
}

/// <summary>A semantic memory: a durable fact or learned rule.</summary>
public sealed class SemanticMemory
{
    public required string Key { get; init; }
    public required string Value { get; set; }
    public double Strength { get; set; } = 1.0;        // reinforcement weight
    public long LastSeen { get; set; }
}

/// <summary>A working-memory slot: short-term context currently being processed.</summary>
public sealed class WorkingMemorySlot
{
    public required string Key { get; init; }
    public required object Value { get; init; }
    public long Timestamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}

/// <summary>
/// The brain's memory system: working memory (short-term), episodic memory
/// (event recall with similarity search), and semantic memory (durable facts).
/// </summary>
public sealed class MemorySystem
{
    private readonly List<EpisodicMemory> _episodic = new();
    private readonly Dictionary<string, SemanticMemory> _semantic = new();
    private readonly Dictionary<string, WorkingMemorySlot> _working = new();
    private readonly int _maxEpisodic;
    private readonly int _maxWorking;
    private long _nextId = 1;
    private readonly object _lock = new();

    public MemorySystem(int maxEpisodic = 10000, int maxWorking = 128)
    {
        _maxEpisodic = maxEpisodic;
        _maxWorking = maxWorking;
    }

    // ---- Working memory ----
    public void SetWorking(string key, object value)
    {
        lock (_lock)
        {
            _working[key] = new WorkingMemorySlot { Key = key, Value = value };
            if (_working.Count > _maxWorking)
            {
                var oldest = _working.Values.OrderBy(s => s.Timestamp).First();
                _working.Remove(oldest.Key);
            }
        }
    }

    public object? GetWorking(string key)
    {
        lock (_lock) return _working.TryGetValue(key, out var s) ? s.Value : null;
    }

    public IEnumerable<WorkingMemorySlot> WorkingSnapshot()
    {
        lock (_lock) return _working.Values.ToList();
    }

    // ---- Episodic memory ----
    public long Remember(EpisodicMemory mem)
    {
        lock (_lock)
        {
            mem.Id = _nextId++;
            mem.Epoch = mem.Epoch == 0 ? DateTimeOffset.UtcNow.ToUnixTimeSeconds() : mem.Epoch;
            _episodic.Add(mem);
            if (_episodic.Count > _maxEpisodic) _episodic.RemoveAt(0);
            return mem.Id;
        }
    }

    /// <summary>Recall memories by event type and symbol.</summary>
    public List<EpisodicMemory> Recall(string? eventType = null, string? symbol = null, int limit = 50)
    {
        lock (_lock)
        {
            return _episodic
                .Where(m => eventType is null || m.Event == eventType)
                .Where(m => symbol is null || m.Symbol == symbol)
                .OrderByDescending(m => m.Epoch)
                .Take(limit)
                .ToList();
        }
    }

    /// <summary>Similarity-based recall using a query signature (cosine similarity).</summary>
    public List<(EpisodicMemory Mem, double Similarity)> RecallSimilar(double[] query, int limit = 10)
    {
        lock (_lock)
        {
            var results = new List<(EpisodicMemory, double)>();
            foreach (var m in _episodic)
            {
                if (m.Signature is null || m.Signature.Length != query.Length) continue;
                double sim = Cosine(query, m.Signature);
                results.Add((m, sim));
            }
            return results.OrderByDescending(r => r.Item2).Take(limit).ToList();
        }
    }

    /// <summary>Update the outcome/reward of a remembered event (reinforcement).</summary>
    public void UpdateOutcome(long id, double outcome)
    {
        lock (_lock)
        {
            var m = _episodic.FirstOrDefault(x => x.Id == id);
            if (m is not null) m.Outcome = outcome;
        }
    }

    // ---- Semantic memory ----
    public void StoreFact(string key, string value, double strength = 1.0)
    {
        lock (_lock)
        {
            if (_semantic.TryGetValue(key, out var existing))
            {
                existing.Value = value;
                existing.Strength = Math.Min(2.0, existing.Strength + 0.1);
                existing.LastSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
            else
            {
                _semantic[key] = new SemanticMemory
                {
                    Key = key,
                    Value = value,
                    Strength = strength,
                    LastSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
            }
        }
    }

    public string? GetFact(string key)
    {
        lock (_lock) return _semantic.TryGetValue(key, out var s) ? s.Value : null;
    }

    public IEnumerable<SemanticMemory> AllFacts()
    {
        lock (_lock) return _semantic.Values.OrderByDescending(s => s.Strength).ToList();
    }

    // ---- Persistence ----
    public void Save(string dir)
    {
        Directory.CreateDirectory(dir);
        lock (_lock)
        {
            File.WriteAllText(Path.Combine(dir, "episodic.json"),
                JsonSerializer.Serialize(_episodic, new JsonSerializerOptions { WriteIndented = true }));
            File.WriteAllText(Path.Combine(dir, "semantic.json"),
                JsonSerializer.Serialize(_semantic.Values, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public void Load(string dir)
    {
        try
        {
            var ep = Path.Combine(dir, "episodic.json");
            if (File.Exists(ep))
            {
                var loaded = JsonSerializer.Deserialize<List<EpisodicMemory>>(File.ReadAllText(ep));
                if (loaded is not null)
                {
                    lock (_lock)
                    {
                        _episodic.Clear();
                        _episodic.AddRange(loaded);
                        _nextId = _episodic.Count == 0 ? 1 : _episodic.Max(m => m.Id) + 1;
                    }
                }
            }
            var sem = Path.Combine(dir, "semantic.json");
            if (File.Exists(sem))
            {
                var loaded = JsonSerializer.Deserialize<List<SemanticMemory>>(File.ReadAllText(sem));
                if (loaded is not null)
                {
                    lock (_lock)
                    {
                        _semantic.Clear();
                        foreach (var s in loaded) _semantic[s.Key] = s;
                    }
                }
            }
        }
        catch { /* corrupt memory — start fresh */ }
    }

    public int EpisodicCount { get { lock (_lock) return _episodic.Count; } }
    public int SemanticCount { get { lock (_lock) return _semantic.Count; } }

    private static double Cosine(double[] a, double[] b)
    {
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
        if (na < 1e-12 || nb < 1e-12) return 0;
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }
}
