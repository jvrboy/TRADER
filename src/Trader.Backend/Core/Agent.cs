namespace Trader.Backend.Core;

/// <summary>
/// Registry that holds every tool available to agents. Enables discovery
/// (listing tools) and dispatch (calling a tool by name).
/// </summary>
public sealed class ToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ITool tool) => _tools[tool.Name] = tool;

    public void RegisterRange(IEnumerable<ITool> tools)
    {
        foreach (var t in tools) Register(t);
    }

    public IReadOnlyCollection<ITool> All => _tools.Values;

    public bool TryGet(string name, out ITool? tool) => _tools.TryGetValue(name, out tool);

    public IReadOnlyList<string> Names => _tools.Keys.OrderBy(k => k).ToList();

    public string Describe() =>
        string.Join("\n", _tools.Values
            .OrderBy(t => t.Name)
            .Select(t => $"  {t.Name,-24} ({t.Parameters}) - {t.Description}"));
}

/// <summary>
/// A lightweight agent that can discover tools, plan a small sequence of
/// steps, and execute them against the registry. This is intentionally
/// framework-agnostic so it can run anywhere.
/// </summary>
public sealed class Agent
{
    private readonly ToolRegistry _registry;
    private readonly string _name;

    public Agent(string name, ToolRegistry registry)
    {
        _name = name;
        _registry = registry;
    }

    public string Name => _name;

    /// <summary>Run a single tool by name with the given arguments.</summary>
    public async Task<ToolResult> RunToolAsync(ToolContext context, string toolName, IReadOnlyDictionary<string, string> args)
    {
        if (!_registry.TryGet(toolName, out var tool) || tool is null)
            return ToolResult.Fail($"Unknown tool '{toolName}'. Available: {string.Join(", ", _registry.Names)}");

        context.Log?.Invoke($"[{_name}] calling {toolName}({string.Join(", ", args.Select(a => $"{a.Key}={a.Value}"))})");
        var result = await tool.ExecuteAsync(context, args);
        context.Log?.Invoke($"[{_name}] {toolName} -> {(result.Success ? "OK" : "FAIL")}: {result.Message}");
        return result;
    }

    /// <summary>
    /// Run a scripted plan: a sequence of steps, each naming a tool and its
    /// arguments. Aggregates results for the caller.
    /// </summary>
    public async Task<IReadOnlyList<ToolResult>> RunPlanAsync(ToolContext context, IEnumerable<AgentStep> steps)
    {
        var results = new List<ToolResult>();
        foreach (var step in steps)
        {
            results.Add(await RunToolAsync(context, step.Tool, step.Args));
        }
        return results;
    }
}

/// <summary>One step in an agent plan: which tool to call and with what args.</summary>
public sealed record AgentStep(string Tool, IReadOnlyDictionary<string, string> Args);
