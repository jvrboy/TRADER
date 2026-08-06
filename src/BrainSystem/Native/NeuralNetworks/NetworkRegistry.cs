using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BrainSystem.Core;

namespace BrainSystem.NeuralNetworks;

/// <summary>
/// Holds and orchestrates 1000+ specialised neural networks — one per "cortical column".
/// Each network is registered under a domain/purpose tag, indexed for parallel dispatch.
/// </summary>
public class NetworkRegistry
{
    private readonly ConcurrentDictionary<string, NeuralNetwork> _byId = new();
    private readonly ConcurrentDictionary<string, List<NeuralNetwork>> _byDomain = new();

    public int Count => _byId.Count;
    public IEnumerable<string> Domains => _byDomain.Keys;

    public NeuralNetwork Register(NeuralNetwork nn, string domain)
    {
        _byId[nn.Id] = nn;
        _byDomain.AddOrUpdate(domain, _ => new List<NeuralNetwork> { nn }, (_, l) => { lock (l) l.Add(nn); return l; });
        return nn;
    }

    public NeuralNetwork? Get(string id) => _byId.TryGetValue(id, out var n) ? n : null;
    public IReadOnlyList<NeuralNetwork> InDomain(string domain) =>
        _byDomain.TryGetValue(domain, out var l) ? l : Array.Empty<NeuralNetwork>();

    /// <summary>Bulk-create N networks with a template — used to spin up the 1000+ swarm.</summary>
    public void Populate(string domain, int count, Func<int, (string name, string purpose, int[] layers)> factory, int? seedBase = null)
    {
        Parallel.For(0, count, i =>
        {
            var (name, purpose, layers) = factory(i);
            var nn = new NeuralNetwork(name, purpose, layers, seed: (seedBase ?? 42) + i);
            Register(nn, domain);
        });
    }

    /// <summary>Run every net in a domain over the same input; return list of outputs.</summary>
    public List<(NeuralNetwork nn, float[] output)> BroadcastForward(string domain, float[] input)
    {
        var nets = InDomain(domain);
        var results = new (NeuralNetwork, float[])[nets.Count];
        Parallel.For(0, nets.Count, i =>
        {
            var nn = nets[i];
            if (nn.Layers[0] != input.Length) { results[i] = (nn, Array.Empty<float>()); return; }
            results[i] = (nn, (float[])nn.Forward(input).Clone());
        });
        return results.ToList();
    }

    /// <summary>Ensemble: average outputs of all compatible nets in a domain.</summary>
    public float[] Ensemble(string domain, float[] input)
    {
        var outs = BroadcastForward(domain, input).Where(o => o.output.Length > 0).ToList();
        if (outs.Count == 0) return Array.Empty<float>();
        int dim = outs[0].output.Length;
        var avg = new float[dim];
        foreach (var (_, o) in outs) for (int i = 0; i < dim; i++) avg[i] += o[i];
        for (int i = 0; i < dim; i++) avg[i] /= outs.Count;
        return avg;
    }

    public void SaveAll(string dir)
    {
        Directory.CreateDirectory(dir);
        foreach (var (id, nn) in _byId) File.WriteAllBytes(Path.Combine(dir, id + ".bnn"), nn.Serialize());
    }

    public int LoadAll(string dir, string domain)
    {
        if (!Directory.Exists(dir)) return 0;
        int loaded = 0;
        foreach (var f in Directory.EnumerateFiles(dir, "*.bnn"))
        {
            try
            {
                var nn = NeuralNetwork.Deserialize(File.ReadAllBytes(f));
                Register(nn, domain);
                loaded++;
            }
            catch { }
        }
        return loaded;
    }

    public Dictionary<string, object> Stats() => new()
    {
        ["total_networks"] = Count,
        ["domains"] = Domains.Count(),
        ["by_domain"] = _byDomain.ToDictionary(k => k.Key, v => v.Value.Count),
        ["total_training_steps"] = _byId.Values.Sum(n => n.TrainingSteps),
    };
}
