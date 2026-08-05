using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BrainSystem.Core;

namespace BrainSystem.Memory;

public enum MemoryType { Working, ShortTerm, LongTerm, Episodic, Semantic, Procedural }

public class MemoryRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public MemoryType Type { get; set; }
    public string Content { get; set; } = "";
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public Dictionary<string, string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastAccess { get; set; } = DateTime.UtcNow;
    public int AccessCount { get; set; }
    public float Importance { get; set; } = 0.5f;
    public float Decay { get; set; } = 1.0f;

    public float EffectiveStrength()
    {
        double hoursOld = (DateTime.UtcNow - LastAccess).TotalHours;
        double decay = Math.Exp(-hoursOld / (24.0 * (1.0 + Importance * 30.0)));
        return (float)(Importance * decay * Decay + 0.05f * MathF.Log2(AccessCount + 2));
    }
}

/// <summary>
/// Multi-tier memory system: working / short-term / long-term / episodic / semantic / procedural.
/// Vector search + tag search + decay.
/// </summary>
public class MemorySystem
{
    private readonly ConcurrentDictionary<string, MemoryRecord> _all = new();
    private readonly ConcurrentDictionary<MemoryType, ConcurrentDictionary<string, byte>> _byType = new();
    public int WorkingCapacity { get; set; } = 7;   // Miller's magic number
    public int ShortTermCapacity { get; set; } = 128;

    public MemorySystem()
    {
        foreach (MemoryType mt in Enum.GetValues(typeof(MemoryType)))
            _byType[mt] = new ConcurrentDictionary<string, byte>();
    }

    public int Count => _all.Count;
    public int CountOf(MemoryType t) => _byType[t].Count;

    public MemoryRecord Store(string content, MemoryType type = MemoryType.ShortTerm,
                              float[]? embedding = null, float importance = 0.5f,
                              Dictionary<string, string>? tags = null)
    {
        var rec = new MemoryRecord
        {
            Type = type,
            Content = content,
            Embedding = embedding ?? SimpleEmbed(content),
            Importance = importance,
            Tags = tags ?? new()
        };
        _all[rec.Id] = rec;
        _byType[type][rec.Id] = 0;
        EnforceCapacity(type);
        return rec;
    }

    void EnforceCapacity(MemoryType t)
    {
        int cap = t switch
        {
            MemoryType.Working => WorkingCapacity,
            MemoryType.ShortTerm => ShortTermCapacity,
            _ => int.MaxValue
        };
        var bucket = _byType[t];
        if (bucket.Count <= cap) return;
        var toRemove = _all.Values.Where(r => r.Type == t).OrderBy(r => r.EffectiveStrength()).Take(bucket.Count - cap).ToList();
        foreach (var r in toRemove)
        {
            // Demote instead of destroy
            if (t == MemoryType.Working) Promote(r.Id, MemoryType.ShortTerm);
            else if (t == MemoryType.ShortTerm) Promote(r.Id, MemoryType.LongTerm);
        }
    }

    public void Promote(string id, MemoryType newType)
    {
        if (!_all.TryGetValue(id, out var r)) return;
        _byType[r.Type].TryRemove(id, out _);
        r.Type = newType;
        _byType[newType][id] = 0;
    }

    public MemoryRecord? Recall(string id)
    {
        if (_all.TryGetValue(id, out var r))
        {
            r.LastAccess = DateTime.UtcNow;
            r.AccessCount++;
            return r;
        }
        return null;
    }

    public List<(MemoryRecord rec, float score)> Search(string query, int topK = 5, MemoryType? type = null)
    {
        var q = SimpleEmbed(query);
        var candidates = type.HasValue
            ? _all.Values.Where(r => r.Type == type.Value)
            : _all.Values;
        return candidates
            .Select(r => (r, Similarity(q, r.Embedding) * r.EffectiveStrength()))
            .OrderByDescending(t => t.Item2)
            .Take(topK)
            .Select(t => { t.r.LastAccess = DateTime.UtcNow; t.r.AccessCount++; return t; })
            .ToList();
    }

    static float Similarity(float[] a, float[] b)
    {
        if (a.Length == 0 || b.Length == 0) return 0;
        int n = Math.Min(a.Length, b.Length);
        float dot = 0, na = 0, nb = 0;
        for (int i = 0; i < n; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
        if (na == 0 || nb == 0) return 0;
        return dot / (MathF.Sqrt(na) * MathF.Sqrt(nb));
    }

    /// <summary>Deterministic 128-dim hash-embedding (no dependencies).</summary>
    public static float[] SimpleEmbed(string s, int dim = 128)
    {
        var v = new float[dim];
        if (string.IsNullOrEmpty(s)) return v;
        var tokens = s.ToLowerInvariant().Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var tok in tokens)
        {
            uint h = 2166136261u;
            foreach (var ch in tok) { h ^= ch; h *= 16777619; }
            int idx = (int)(h % (uint)dim);
            int sign = (int)((h >> 16) & 1) == 0 ? 1 : -1;
            v[idx] += sign * (1f / MathF.Sqrt(tokens.Length));
        }
        // L2 normalise
        float n = 0; for (int i = 0; i < dim; i++) n += v[i] * v[i];
        if (n > 0) { float inv = 1f / MathF.Sqrt(n); for (int i = 0; i < dim; i++) v[i] *= inv; }
        return v;
    }

    /// <summary>Consolidation pass — mimics sleep. Moves important STM -> LTM, decays weak.</summary>
    public int Consolidate(float promoteThreshold = 0.65f, float forgetThreshold = 0.05f)
    {
        int moved = 0, forgotten = 0;
        foreach (var r in _all.Values.ToList())
        {
            var s = r.EffectiveStrength();
            if (r.Type == MemoryType.ShortTerm && s > promoteThreshold)
            { Promote(r.Id, MemoryType.LongTerm); moved++; }
            else if (r.Type != MemoryType.LongTerm && r.Type != MemoryType.Semantic && s < forgetThreshold)
            { _all.TryRemove(r.Id, out _); _byType[r.Type].TryRemove(r.Id, out _); forgotten++; }
        }
        return moved + forgotten;
    }

    public void Save(string path)
    {
        var payload = _all.Values.Select(r => new
        {
            r.Id, r.Type, r.Content, r.Embedding, r.Tags, r.CreatedAt, r.LastAccess, r.AccessCount, r.Importance, r.Decay
        });
        File.WriteAllText(path, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false }));
    }

    public int Load(string path)
    {
        if (!File.Exists(path)) return 0;
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var arr = JsonSerializer.Deserialize<List<MemoryRecord>>(File.ReadAllText(path), opts);
        if (arr == null) return 0;
        foreach (var r in arr) { _all[r.Id] = r; _byType[r.Type][r.Id] = 0; }
        return arr.Count;
    }

    public Dictionary<string, object> Stats() => new()
    {
        ["total"] = Count,
        ["working"] = CountOf(MemoryType.Working),
        ["short_term"] = CountOf(MemoryType.ShortTerm),
        ["long_term"] = CountOf(MemoryType.LongTerm),
        ["episodic"] = CountOf(MemoryType.Episodic),
        ["semantic"] = CountOf(MemoryType.Semantic),
        ["procedural"] = CountOf(MemoryType.Procedural),
    };
}
