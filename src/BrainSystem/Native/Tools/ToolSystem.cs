using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrainSystem.Tools;

public class ToolParam { public string Name = ""; public string Type = "string"; public string Description = ""; public bool Required = true; }

public class ToolResult
{
    public bool Success;
    public string Output = "";
    public object? Data;
    public Dictionary<string, object> Meta = new();
    public string? Error;
    public double DurationMs;
}

public abstract class Tool
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual List<ToolParam> Parameters => new();
    public abstract Task<ToolResult> ExecuteAsync(Dictionary<string, object?> args);

    protected static string S(Dictionary<string, object?> a, string k, string d = "")
        => a.TryGetValue(k, out var v) && v != null ? v.ToString()! : d;
    protected static double D(Dictionary<string, object?> a, string k, double d = 0)
        => a.TryGetValue(k, out var v) && v != null && double.TryParse(v.ToString(), out var x) ? x : d;
    protected static int I(Dictionary<string, object?> a, string k, int d = 0)
        => a.TryGetValue(k, out var v) && v != null && int.TryParse(v.ToString(), out var x) ? x : d;
}

public class ToolSystem
{
    private readonly ConcurrentDictionary<string, Tool> _tools = new();
    public int Count => _tools.Count;

    public ToolSystem Register(Tool t) { _tools[t.Name] = t; return this; }
    public IEnumerable<Tool> All => _tools.Values;
    public Tool? Get(string name) => _tools.TryGetValue(name, out var t) ? t : null;

    public async Task<ToolResult> InvokeAsync(string name, Dictionary<string, object?> args)
    {
        if (!_tools.TryGetValue(name, out var t))
            return new ToolResult { Success = false, Error = $"tool '{name}' not registered" };
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var r = await t.ExecuteAsync(args);
            r.DurationMs = sw.Elapsed.TotalMilliseconds;
            return r;
        }
        catch (Exception ex)
        {
            return new ToolResult { Success = false, Error = ex.Message, DurationMs = sw.Elapsed.TotalMilliseconds };
        }
    }

    public string DescribeForLlm()
    {
        var lines = new List<string> { "Available tools:" };
        foreach (var t in _tools.Values.OrderBy(t => t.Name))
        {
            lines.Add($"- {t.Name}: {t.Description}");
            foreach (var p in t.Parameters)
                lines.Add($"    • {p.Name} ({p.Type}){(p.Required ? "*" : "")} — {p.Description}");
        }
        return string.Join("\n", lines);
    }
}
