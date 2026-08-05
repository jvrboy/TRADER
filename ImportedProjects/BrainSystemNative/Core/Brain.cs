using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BrainSystem.Functions;
using BrainSystem.LLM;
using BrainSystem.Memory;
using BrainSystem.NeuralNetworks;
using BrainSystem.Tools;

namespace BrainSystem.Core;

/// <summary>
/// The top-level Brain — wires the network registry, memory system, LLM runner, tools & functions.
/// </summary>
public class Brain
{
    public NetworkRegistry Networks { get; } = new();
    public MemorySystem Memory { get; } = new();
    public KnowledgeGraph Knowledge { get; } = new();
    public ToolSystem Tools { get; } = new();
    public FunctionRegistry Functions { get; } = FunctionRegistry.BuildDefault();
    public GgufRunner? Llm { get; private set; }

    public DateTime StartTime { get; } = DateTime.UtcNow;

    public Brain(bool buildDefaultBrain = true)
    {
        if (buildDefaultBrain)
        {
            Console.WriteLine("[Brain] Building 1000+ neural networks…");
            var t0 = DateTime.UtcNow;
            int n = BrainFactory.BuildDefaultBrain(Networks);
            Console.WriteLine($"[Brain] {n} networks across {Networks.Domains.Count()} regions ready in {(DateTime.UtcNow - t0).TotalMilliseconds:F0} ms.");
        }
        RegisterBuiltinTools();
    }

    void RegisterBuiltinTools()
    {
        Tools.Register(new CalculatorTool());
        Tools.Register(new DateTimeTool());
        Tools.Register(new FileReadTool());
        Tools.Register(new FileWriteTool());
        Tools.Register(new ListDirTool());
        Tools.Register(new HttpFetchTool());
        Tools.Register(new RegexTool());
        Tools.Register(new MemoryStoreTool(Memory));
        Tools.Register(new MemoryRecallTool(Memory));
        Tools.Register(new BrainForwardTool(Networks));
        Tools.Register(new StatsTool(Snapshot));
        Tools.Register(new ShellEchoTool());
        Tools.Register(new RandomTool());
    }

    public void LoadGguf(string path)
    {
        Console.WriteLine($"[Brain] Loading GGUF model: {path}");
        Llm = new GgufRunner(path);
        var info = Llm.Info();
        foreach (var (k, v) in info) Console.WriteLine($"  {k}: {v}");
    }

    /// <summary>
    /// End-to-end query: memory-recall → optional LLM → memory-store. This is the
    /// brain's "think" cycle. Any tool the caller lists can be invoked by name.
    /// </summary>
    public async Task<string> ThinkAsync(string prompt, int memK = 3)
    {
        var hits = Memory.Search(prompt, memK).Select(h => h.rec.Content).ToList();
        string answer;
        if (Llm != null)
            answer = Llm.GenerateWithContext(prompt, hits, maxTokens: 96);
        else
            answer = SymbolicAnswer(prompt, hits);
        Memory.Store($"Q: {prompt}\nA: {answer}", MemoryType.Episodic, importance: 0.6f);
        return answer;
    }

    string SymbolicAnswer(string prompt, List<string> hits)
    {
        if (hits.Count > 0)
            return $"[recall] Based on {hits.Count} memories: " + string.Join(" | ", hits.Take(3));
        return $"[brain] I don't have a stored answer for '{prompt}'. Consider training a network or storing a memory.";
    }

    /// <summary>Train a specific region on labelled samples.</summary>
    public float TrainRegion(string domain, List<(float[] x, float[] y)> samples, int epochs = 5)
    {
        var nets = Networks.InDomain(domain);
        if (nets.Count == 0) return -1;
        float lastLoss = 0;
        for (int e = 0; e < epochs; e++)
        {
            float loss = 0;
            Parallel.ForEach(nets, nn =>
            {
                if (nn.Layers[0] != samples[0].x.Length || nn.Layers[^1] != samples[0].y.Length) return;
                foreach (var (x, y) in samples) loss += nn.TrainStep(x, y);
            });
            lastLoss = loss / (nets.Count * samples.Count);
        }
        return lastLoss;
    }

    public Dictionary<string, object> Snapshot() => new()
    {
        ["uptime_seconds"] = (DateTime.UtcNow - StartTime).TotalSeconds,
        ["networks"] = Networks.Stats(),
        ["memory"] = Memory.Stats(),
        ["knowledge_nodes"] = Knowledge.NodeCount,
        ["knowledge_edges"] = Knowledge.EdgeCount,
        ["tools"] = Tools.Count,
        ["functions"] = Functions.Count,
        ["llm_loaded"] = Llm != null,
        ["llm"] = Llm?.Info() as object ?? "",
    };

    public void Save(string dir)
    {
        Directory.CreateDirectory(dir);
        Networks.SaveAll(Path.Combine(dir, "networks"));
        Memory.Save(Path.Combine(dir, "memory.json"));
        Knowledge.Save(Path.Combine(dir, "knowledge.json"));
        Console.WriteLine($"[Brain] Saved to {dir}");
    }
}
