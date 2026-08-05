using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrainSystem.Tools;

public class CalculatorTool : Tool
{
    public override string Name => "calculator";
    public override string Description => "Evaluate a numeric/math expression (+ - * / ^ sin cos sqrt log exp %).";
    public override List<ToolParam> Parameters => new() { new() { Name = "expression", Type = "string", Description = "Math expression, e.g. 2*(3+4)/5" } };
    public override Task<ToolResult> ExecuteAsync(Dictionary<string, object?> args)
    {
        var expr = S(args, "expression");
        try
        {
            var e = expr.Replace(" ", "");
            e = Regex.Replace(e, @"sqrt\(([^)]+)\)", m => $"({Math.Sqrt(EvalPow(m.Groups[1].Value))})");
            e = Regex.Replace(e, @"sin\(([^)]+)\)",  m => $"({Math.Sin(EvalPow(m.Groups[1].Value))})");
            e = Regex.Replace(e, @"cos\(([^)]+)\)",  m => $"({Math.Cos(EvalPow(m.Groups[1].Value))})");
            e = Regex.Replace(e, @"log\(([^)]+)\)",  m => $"({Math.Log(EvalPow(m.Groups[1].Value))})");
            e = Regex.Replace(e, @"exp\(([^)]+)\)",  m => $"({Math.Exp(EvalPow(m.Groups[1].Value))})");
            // Resolve ^ manually (right-associative) then hand + - * / % / parens to DataTable.
            while (Regex.IsMatch(e, @"(\d+(?:\.\d+)?|\([^()]*\))\^(\d+(?:\.\d+)?|\([^()]*\))"))
                e = Regex.Replace(e, @"(\d+(?:\.\d+)?|\([^()]*\))\^(\d+(?:\.\d+)?|\([^()]*\))",
                    m => "(" + Math.Pow(EvalPow(m.Groups[1].Value), EvalPow(m.Groups[2].Value)).ToString(System.Globalization.CultureInfo.InvariantCulture) + ")");
            var dt = new DataTable();
            var val = dt.Compute(e, "");
            return Task.FromResult(new ToolResult { Success = true, Output = val?.ToString() ?? "", Data = val });
        }
        catch (Exception ex) { return Task.FromResult(new ToolResult { Success = false, Error = ex.Message }); }
    }

    static double EvalPow(string s)
    {
        s = s.Trim('(', ')');
        if (double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d)) return d;
        return Convert.ToDouble(new DataTable().Compute(s, ""), System.Globalization.CultureInfo.InvariantCulture);
    }
}

public class DateTimeTool : Tool
{
    public override string Name => "datetime";
    public override string Description => "Get the current UTC or local date/time, or diff two dates.";
    public override List<ToolParam> Parameters => new()
    {
        new() { Name = "op", Type = "string", Description = "now | utc | diff", Required = false },
        new() { Name = "a", Type = "string", Description = "date A (for diff)", Required = false },
        new() { Name = "b", Type = "string", Description = "date B (for diff)", Required = false }
    };
    public override Task<ToolResult> ExecuteAsync(Dictionary<string, object?> args)
    {
        var op = S(args, "op", "utc");
        if (op == "diff")
        {
            var a = DateTime.Parse(S(args, "a"));
            var b = DateTime.Parse(S(args, "b"));
            return Task.FromResult(new ToolResult { Success = true, Output = (b - a).ToString(), Data = (b - a) });
        }
        var dt = op == "now" ? DateTime.Now : DateTime.UtcNow;
        return Task.FromResult(new ToolResult { Success = true, Output = dt.ToString("O"), Data = dt });
    }
}

public class FileReadTool : Tool
{
    public override string Name => "file_read";
    public override string Description => "Read a text file from disk.";
    public override List<ToolParam> Parameters => new() { new() { Name = "path", Type = "string", Description = "Absolute or relative path" } };
    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object?> args)
    {
        var p = S(args, "path");
        if (!File.Exists(p)) return new ToolResult { Success = false, Error = "not found" };
        var text = await File.ReadAllTextAsync(p);
        return new ToolResult { Success = true, Output = text, Meta = new() { ["bytes"] = new FileInfo(p).Length } };
    }
}

public class FileWriteTool : Tool
{
    public override string Name => "file_write";
    public override string Description => "Write text to a file (creates directories).";
    public override List<ToolParam> Parameters => new()
    {
        new() { Name = "path", Type = "string", Description = "Target path" },
        new() { Name = "content", Type = "string", Description = "Text to write" },
        new() { Name = "append", Type = "bool", Description = "Append instead of overwrite", Required = false }
    };
    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object?> args)
    {
        var p = S(args, "path");
        var c = S(args, "content");
        var append = S(args, "append", "false").ToLowerInvariant() == "true";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(p))!);
        if (append) await File.AppendAllTextAsync(p, c); else await File.WriteAllTextAsync(p, c);
        return new ToolResult { Success = true, Output = $"wrote {c.Length} chars to {p}" };
    }
}

public class ListDirTool : Tool
{
    public override string Name => "list_dir";
    public override string Description => "List files / subdirs in a directory.";
    public override List<ToolParam> Parameters => new() { new() { Name = "path", Type = "string", Description = "Directory path" } };
    public override Task<ToolResult> ExecuteAsync(Dictionary<string, object?> args)
    {
        var p = S(args, "path", ".");
        if (!Directory.Exists(p)) return Task.FromResult(new ToolResult { Success = false, Error = "no such dir" });
        var files = Directory.EnumerateFileSystemEntries(p).ToArray();
        return Task.FromResult(new ToolResult { Success = true, Output = string.Join("\n", files), Data = files });
    }
}

public class HttpFetchTool : Tool
{
    static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    public override string Name => "http_fetch";
    public override string Description => "HTTP GET a URL and return the body (first 32 KB).";
    public override List<ToolParam> Parameters => new() { new() { Name = "url", Type = "string", Description = "http(s) URL" } };
    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object?> args)
    {
        var u = S(args, "url");
        try
        {
            var resp = await _http.GetAsync(u);
            var body = await resp.Content.ReadAsStringAsync();
            if (body.Length > 32768) body = body[..32768];
            return new ToolResult { Success = resp.IsSuccessStatusCode, Output = body, Meta = new() { ["status"] = (int)resp.StatusCode } };
        }
        catch (Exception ex) { return new ToolResult { Success = false, Error = ex.Message }; }
    }
}

public class RegexTool : Tool
{
    public override string Name => "regex";
    public override string Description => "Apply a regex to text (find/replace).";
    public override List<ToolParam> Parameters => new()
    {
        new() { Name = "text", Type = "string", Description = "Input text" },
        new() { Name = "pattern", Type = "string", Description = ".NET regex" },
        new() { Name = "replacement", Type = "string", Description = "Optional replacement", Required = false },
    };
    public override Task<ToolResult> ExecuteAsync(Dictionary<string, object?> args)
    {
        var text = S(args, "text");
        var pat = S(args, "pattern");
        if (args.ContainsKey("replacement"))
        {
            var repl = S(args, "replacement");
            return Task.FromResult(new ToolResult { Success = true, Output = Regex.Replace(text, pat, repl) });
        }
        var matches = Regex.Matches(text, pat).Select(m => m.Value).ToArray();
        return Task.FromResult(new ToolResult { Success = true, Output = string.Join("\n", matches), Data = matches });
    }
}

public class MemoryStoreTool : Tool
{
    private readonly Memory.MemorySystem _mem;
    public MemoryStoreTool(Memory.MemorySystem m) { _mem = m; }
    public override string Name => "memory_store";
    public override string Description => "Store a piece of text into the brain's memory system.";
    public override List<ToolParam> Parameters => new()
    {
        new() { Name = "content", Type = "string", Description = "Text to remember" },
        new() { Name = "type", Type = "string", Description = "Working|ShortTerm|LongTerm|Episodic|Semantic|Procedural", Required = false },
        new() { Name = "importance", Type = "float", Description = "0..1", Required = false },
    };
    public override Task<ToolResult> ExecuteAsync(Dictionary<string, object?> args)
    {
        var c = S(args, "content");
        var t = Enum.TryParse<Memory.MemoryType>(S(args, "type", "ShortTerm"), true, out var mt) ? mt : Memory.MemoryType.ShortTerm;
        var imp = (float)D(args, "importance", 0.5);
        var r = _mem.Store(c, t, importance: imp);
        return Task.FromResult(new ToolResult { Success = true, Output = r.Id, Data = r });
    }
}

public class MemoryRecallTool : Tool
{
    private readonly Memory.MemorySystem _mem;
    public MemoryRecallTool(Memory.MemorySystem m) { _mem = m; }
    public override string Name => "memory_recall";
    public override string Description => "Semantic-search the brain's memory system.";
    public override List<ToolParam> Parameters => new()
    {
        new() { Name = "query", Type = "string", Description = "Question / cue" },
        new() { Name = "top_k", Type = "int", Description = "How many hits", Required = false }
    };
    public override Task<ToolResult> ExecuteAsync(Dictionary<string, object?> args)
    {
        var q = S(args, "query");
        int k = I(args, "top_k", 5);
        var hits = _mem.Search(q, k);
        var sb = new StringBuilder();
        foreach (var (r, sc) in hits) sb.AppendLine($"[{sc:F3}] {r.Type} — {r.Content}");
        return Task.FromResult(new ToolResult { Success = true, Output = sb.ToString(), Data = hits });
    }
}

public class BrainForwardTool : Tool
{
    private readonly NeuralNetworks.NetworkRegistry _reg;
    public BrainForwardTool(NeuralNetworks.NetworkRegistry r) { _reg = r; }
    public override string Name => "brain_forward";
    public override string Description => "Run an input vector through an entire brain region and get the ensemble output.";
    public override List<ToolParam> Parameters => new()
    {
        new() { Name = "domain", Type = "string", Description = "Region name (e.g. visual_cortex)" },
        new() { Name = "input", Type = "csv", Description = "Comma-separated floats matching the region's input size" }
    };
    public override Task<ToolResult> ExecuteAsync(Dictionary<string, object?> args)
    {
        var d = S(args, "domain");
        var raw = S(args, "input");
        var vals = raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(x => float.Parse(x.Trim())).ToArray();
        var o = _reg.Ensemble(d, vals);
        return Task.FromResult(new ToolResult { Success = o.Length > 0, Output = string.Join(",", o.Select(v => v.ToString("F4"))), Data = o });
    }
}

public class StatsTool : Tool
{
    private readonly Func<Dictionary<string, object>> _snapshot;
    public StatsTool(Func<Dictionary<string, object>> snap) { _snapshot = snap; }
    public override string Name => "system_stats";
    public override string Description => "Return statistics on the whole BrainSystem (networks, memory, tools).";
    public override Task<ToolResult> ExecuteAsync(Dictionary<string, object?> args)
    {
        var s = _snapshot();
        return Task.FromResult(new ToolResult { Success = true, Output = System.Text.Json.JsonSerializer.Serialize(s), Data = s });
    }
}

public class ShellEchoTool : Tool
{
    public override string Name => "echo";
    public override string Description => "Echo a string (sanity check).";
    public override List<ToolParam> Parameters => new() { new() { Name = "text", Type = "string", Description = "text to echo" } };
    public override Task<ToolResult> ExecuteAsync(Dictionary<string, object?> args)
        => Task.FromResult(new ToolResult { Success = true, Output = S(args, "text") });
}

public class RandomTool : Tool
{
    public override string Name => "random";
    public override string Description => "Generate a random number in [min,max].";
    public override List<ToolParam> Parameters => new()
    {
        new() { Name = "min", Type = "float", Description = "min", Required = false },
        new() { Name = "max", Type = "float", Description = "max", Required = false }
    };
    static readonly Random _rng = new();
    public override Task<ToolResult> ExecuteAsync(Dictionary<string, object?> args)
    {
        double a = D(args, "min", 0), b = D(args, "max", 1);
        double v = _rng.NextDouble() * (b - a) + a;
        return Task.FromResult(new ToolResult { Success = true, Output = v.ToString("F6"), Data = v });
    }
}
