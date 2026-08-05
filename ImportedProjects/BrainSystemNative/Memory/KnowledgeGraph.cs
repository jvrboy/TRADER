using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BrainSystem.Memory;

public class KGNode { public string Id = ""; public string Label = ""; public Dictionary<string, string> Props = new(); }
public class KGEdge { public string From = ""; public string To = ""; public string Relation = ""; public float Weight = 1f; }

/// <summary>Semantic knowledge graph for structured knowledge.</summary>
public class KnowledgeGraph
{
    readonly ConcurrentDictionary<string, KGNode> _nodes = new();
    readonly List<KGEdge> _edges = new();
    readonly object _edgeLock = new();

    public KGNode AddNode(string label, Dictionary<string, string>? props = null)
    {
        var id = label.ToLowerInvariant().Replace(' ', '_');
        return _nodes.GetOrAdd(id, _ => new KGNode { Id = id, Label = label, Props = props ?? new() });
    }

    public void Link(string fromLabel, string relation, string toLabel, float weight = 1f)
    {
        var f = AddNode(fromLabel); var t = AddNode(toLabel);
        lock (_edgeLock) _edges.Add(new KGEdge { From = f.Id, To = t.Id, Relation = relation, Weight = weight });
    }

    public IEnumerable<(KGEdge edge, KGNode target)> Neighbours(string label, string? relation = null)
    {
        var id = label.ToLowerInvariant().Replace(' ', '_');
        lock (_edgeLock)
            foreach (var e in _edges)
                if (e.From == id && (relation == null || e.Relation == relation) && _nodes.TryGetValue(e.To, out var n))
                    yield return (e, n);
    }

    public int NodeCount => _nodes.Count;
    public int EdgeCount { get { lock (_edgeLock) return _edges.Count; } }

    public void Save(string path)
    {
        var payload = new { nodes = _nodes.Values, edges = _edges };
        File.WriteAllText(path, JsonSerializer.Serialize(payload));
    }
}
