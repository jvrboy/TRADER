using System.Collections.Concurrent;

namespace Brain.Tools;

/// <summary>
/// Registry for all available tools. Routes tool calls to the correct implementation.
/// </summary>
public sealed class ToolRegistry
{
    private readonly ConcurrentDictionary<string, ITool> _tools = new();

    public void Register(ITool tool)
    {
        _tools[tool.Name] = tool;
    }

    public void Unregister(string name)
    {
        _tools.TryRemove(name, out _);
    }

    public ITool? GetTool(string name)
    {
        return _tools.TryGetValue(name, out var tool) ? tool : null;
    }

    public IReadOnlyList<ITool> GetAllTools() => _tools.Values.ToArray();

    public IReadOnlyList<string> GetToolNames() => _tools.Keys.ToArray();

    /// <summary>
    /// Executes a tool by name with the given parameters.
    /// </summary>
    public async Task<ToolResult> ExecuteAsync(string toolName, Dictionary<string, object> parameters)
    {
        var tool = GetTool(toolName);
        if (tool == null)
            return ToolResult.Fail("Tool not found: " + toolName);

        try
        {
            return await tool.ExecuteAsync(parameters);
        }
        catch (Exception ex)
        {
            return ToolResult.Fail("Tool execution error: " + ex.Message);
        }
    }

    /// <summary>
    /// Returns a description of all available tools for the LLM prompt.
    /// </summary>
    public string GetToolsDescription()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Available tools:");
        foreach (var tool in _tools.Values)
        {
            sb.AppendLine("- " + tool.Name + ": " + tool.Description);
            sb.AppendLine("  Parameters: " + tool.ParameterSchema);
        }
        return sb.ToString();
    }
}
