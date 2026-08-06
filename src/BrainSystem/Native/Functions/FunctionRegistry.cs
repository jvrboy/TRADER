using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BrainSystem.Functions;

/// <summary>
/// Named native C# functions (delegate-based) callable by tools, the LLM, or the shell.
/// Provides a lighter-weight surface than the Tool subclass API — great for one-liners.
/// </summary>
public class FunctionRegistry
{
    public delegate object? BrainFunction(Dictionary<string, object?> args);

    private readonly ConcurrentDictionary<string, (BrainFunction fn, string desc)> _fns = new();

    public int Count => _fns.Count;
    public IEnumerable<(string Name, string Desc)> All => _fns.Select(kv => (kv.Key, kv.Value.desc));

    public FunctionRegistry Register(string name, string description, BrainFunction fn)
    {
        _fns[name] = (fn, description);
        return this;
    }

    public object? Invoke(string name, Dictionary<string, object?>? args = null)
    {
        if (!_fns.TryGetValue(name, out var e)) throw new KeyNotFoundException($"fn '{name}'");
        return e.fn(args ?? new());
    }

    public static FunctionRegistry BuildDefault()
    {
        var r = new FunctionRegistry();
        r.Register("hello", "Greet someone", a => $"Hello, {a.GetValueOrDefault("name") ?? "world"}!");
        r.Register("add", "Add two numbers", a =>
            Convert.ToDouble(a["a"]) + Convert.ToDouble(a["b"]));
        r.Register("multiply", "Multiply two numbers", a =>
            Convert.ToDouble(a["a"]) * Convert.ToDouble(a["b"]));
        r.Register("upper", "Uppercase text", a => a["text"]?.ToString()?.ToUpper() ?? "");
        r.Register("reverse", "Reverse a string", a => new string((a["text"]?.ToString() ?? "").Reverse().ToArray()));
        r.Register("length", "Length of a string", a => (a["text"]?.ToString() ?? "").Length);
        r.Register("sum", "Sum a comma-separated list", a =>
            (a["list"]?.ToString() ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                                          .Select(double.Parse).Sum());
        r.Register("mean", "Mean of a comma-separated list", a =>
            (a["list"]?.ToString() ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                                          .Select(double.Parse).Average());
        r.Register("clamp", "Clamp value into [lo,hi]", a =>
            Math.Clamp(Convert.ToDouble(a["v"]), Convert.ToDouble(a["lo"]), Convert.ToDouble(a["hi"])));
        r.Register("now", "Current UTC ISO time", _ => DateTime.UtcNow.ToString("O"));
        return r;
    }
}
