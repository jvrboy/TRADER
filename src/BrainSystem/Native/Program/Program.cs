using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BrainSystem.Core;
using BrainSystem.Memory;

namespace BrainSystem;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=====================================================");
        Console.WriteLine("  BRAIN SYSTEM — Native C# Neural / Memory / LLM AI  ");
        Console.WriteLine("=====================================================");

        var brain = new Brain(buildDefaultBrain: true);

        // Seed some knowledge
        brain.Knowledge.Link("Brain", "contains", "NeuralNetwork");
        brain.Knowledge.Link("NeuralNetwork", "uses", "Weights");
        brain.Knowledge.Link("Brain", "has", "Memory");
        brain.Knowledge.Link("Memory", "type", "Episodic");
        brain.Knowledge.Link("Memory", "type", "Semantic");
        brain.Knowledge.Link("Brain", "runs", "LLM");
        brain.Knowledge.Link("LLM", "format", "GGUF");

        brain.Memory.Store("The BrainSystem is a native C# cognitive architecture.", MemoryType.Semantic, importance: 0.9f);
        brain.Memory.Store("Neural networks are organised as cortical regions.", MemoryType.Semantic, importance: 0.85f);
        brain.Memory.Store("GGUF files hold quantised LLM weights.", MemoryType.Semantic, importance: 0.8f);

        // Parse command-line
        string? gguf = null;
        bool interactive = true;
        bool selfTest = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--gguf": gguf = args[++i]; break;
                case "--no-repl": interactive = false; break;
                case "--test": selfTest = true; break;
            }
        }

        if (gguf != null && File.Exists(gguf)) brain.LoadGguf(gguf);
        else if (gguf != null) Console.WriteLine($"[Brain] GGUF path not found: {gguf} (continuing without LLM)");

        if (selfTest) { await SelfTest(brain); return 0; }
        if (!interactive) { await SelfTest(brain); return 0; }

        Console.WriteLine();
        Console.WriteLine("Type a message to talk to the brain, or a slash-command:");
        Console.WriteLine("  /stats                 show system snapshot");
        Console.WriteLine("  /tools                 list registered tools");
        Console.WriteLine("  /tool <name> k=v k=v   invoke a tool");
        Console.WriteLine("  /fn <name> k=v k=v     invoke a native function");
        Console.WriteLine("  /mem <query>           semantic memory search");
        Console.WriteLine("  /store <text>          store a memory");
        Console.WriteLine("  /region <name>         info about a brain region");
        Console.WriteLine("  /consolidate           run a sleep-consolidation pass");
        Console.WriteLine("  /save <dir>            save state");
        Console.WriteLine("  /gguf <path>           load a GGUF model");
        Console.WriteLine("  /quit                  exit");
        Console.WriteLine();

        while (true)
        {
            Console.Write("brain> ");
            var line = Console.ReadLine();
            if (line == null) break;
            line = line.Trim();
            if (line.Length == 0) continue;
            if (line == "/quit" || line == "/exit") break;

            try { await HandleLine(brain, line); }
            catch (Exception ex) { Console.WriteLine($"! {ex.Message}"); }
        }
        return 0;
    }

    static async Task HandleLine(Brain brain, string line)
    {
        if (!line.StartsWith("/")) { Console.WriteLine(await brain.ThinkAsync(line)); return; }

        var parts = line.Split(' ', 2);
        var cmd = parts[0];
        var rest = parts.Length > 1 ? parts[1] : "";

        switch (cmd)
        {
            case "/stats":
                Console.WriteLine(JsonSerializer.Serialize(brain.Snapshot(), new JsonSerializerOptions { WriteIndented = true }));
                break;
            case "/tools":
                Console.WriteLine(brain.Tools.DescribeForLlm());
                break;
            case "/tool":
                {
                    var tp = rest.Split(' ', 2);
                    var tn = tp[0];
                    var kv = ParseKV(tp.Length > 1 ? tp[1] : "");
                    var r = await brain.Tools.InvokeAsync(tn, kv);
                    Console.WriteLine(r.Success ? r.Output : "! " + r.Error);
                    Console.WriteLine($"[{r.DurationMs:F1} ms]");
                    break;
                }
            case "/fn":
                {
                    var tp = rest.Split(' ', 2);
                    var kv = ParseKV(tp.Length > 1 ? tp[1] : "");
                    var res = brain.Functions.Invoke(tp[0], kv);
                    Console.WriteLine(res);
                    break;
                }
            case "/mem":
                {
                    var hits = brain.Memory.Search(rest, 5);
                    foreach (var (r, s) in hits) Console.WriteLine($"  [{s:F3}] {r.Type} — {r.Content}");
                    break;
                }
            case "/store":
                brain.Memory.Store(rest, MemoryType.ShortTerm, importance: 0.6f);
                Console.WriteLine("stored.");
                break;
            case "/region":
                {
                    var nets = brain.Networks.InDomain(rest);
                    Console.WriteLine($"{rest}: {nets.Count} networks");
                    if (nets.Count > 0)
                    {
                        var e = nets[0];
                        Console.WriteLine($"  layers={string.Join("x", e.Layers)}");
                        Console.WriteLine($"  activations={string.Join(",", e.Activations)}");
                    }
                    break;
                }
            case "/consolidate":
                Console.WriteLine($"consolidated {brain.Memory.Consolidate()} items.");
                break;
            case "/save":
                brain.Save(string.IsNullOrEmpty(rest) ? "brain-state" : rest);
                break;
            case "/gguf":
                brain.LoadGguf(rest);
                break;
            default:
                Console.WriteLine("unknown command");
                break;
        }
    }

    static Dictionary<string, object?> ParseKV(string s)
    {
        var d = new Dictionary<string, object?>();
        if (string.IsNullOrWhiteSpace(s)) return d;
        // split by spaces respecting quoted values
        int i = 0;
        while (i < s.Length)
        {
            while (i < s.Length && s[i] == ' ') i++;
            int eq = s.IndexOf('=', i);
            if (eq < 0) break;
            var key = s.Substring(i, eq - i).Trim();
            i = eq + 1;
            string val;
            if (i < s.Length && s[i] == '"')
            {
                int end = s.IndexOf('"', i + 1);
                if (end < 0) { val = s[(i + 1)..]; i = s.Length; }
                else { val = s.Substring(i + 1, end - i - 1); i = end + 1; }
            }
            else
            {
                int sp = s.IndexOf(' ', i);
                if (sp < 0) { val = s[i..]; i = s.Length; }
                else { val = s.Substring(i, sp - i); i = sp; }
            }
            d[key] = val;
        }
        return d;
    }

    static async Task SelfTest(Brain brain)
    {
        Console.WriteLine("\n--- SELF-TEST ---");
        var snap = brain.Snapshot();
        Console.WriteLine("Snapshot: " + JsonSerializer.Serialize(snap));

        // 1. Networks: forward pass on visual cortex
        var input = new float[64]; for (int i = 0; i < 64; i++) input[i] = MathF.Sin(i * 0.1f);
        var ens = brain.Networks.Ensemble("visual_cortex", input);
        Console.WriteLine($"visual_cortex ensemble out[0..4]: {string.Join(",", ens.Take(4).Select(v => v.ToString("F3")))}");

        // 2. Train a small region
        var region = "meta_controller";
        var nets = brain.Networks.InDomain(region);
        Console.WriteLine($"training {region} ({nets.Count} nets)");
        var samples = new List<(float[], float[])>();
        var rng = new Random(1);
        for (int k = 0; k < 32; k++)
        {
            var x = new float[64]; for (int j = 0; j < 64; j++) x[j] = (float)rng.NextDouble();
            var y = new float[16]; for (int j = 0; j < 16; j++) y[j] = (x[j] > 0.5f ? 1f : 0f);
            samples.Add((x, y));
        }
        var loss = brain.TrainRegion(region, samples, epochs: 3);
        Console.WriteLine($"  final avg loss: {loss:F4}");

        // 3. Tools
        var calc = await brain.Tools.InvokeAsync("calculator", new() { ["expression"] = "sqrt(144) + 2*(3+4)^2" });
        Console.WriteLine("calc: " + calc.Output);
        var rnd = await brain.Tools.InvokeAsync("random", new() { ["min"] = "10", ["max"] = "20" });
        Console.WriteLine("rand: " + rnd.Output);

        // 4. Memory
        brain.Memory.Store("Paris is the capital of France.", MemoryType.Semantic, importance: 0.9f);
        brain.Memory.Store("Water boils at 100°C at sea level.", MemoryType.Semantic, importance: 0.9f);
        var mem = brain.Memory.Search("France capital", 3);
        Console.WriteLine("memory hit: " + (mem.FirstOrDefault().rec?.Content ?? "(none)"));

        // 5. Think
        var t = await brain.ThinkAsync("Tell me about the brain system.");
        Console.WriteLine("think → " + t);

        // 6. Functions
        Console.WriteLine("fn add(3,4)= " + brain.Functions.Invoke("add", new() { ["a"] = 3, ["b"] = 4 }));

        Console.WriteLine("\n--- SELF-TEST COMPLETE ---\n");
    }
}
