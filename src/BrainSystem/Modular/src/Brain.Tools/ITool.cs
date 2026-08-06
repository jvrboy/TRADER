namespace Brain.Tools;

/// <summary>
/// Interface for all tools that the agent can invoke.
/// </summary>
public interface ITool
{
    /// <summary>
    /// Unique name of the tool (used in tool calls).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Human-readable description of what the tool does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// JSON schema describing the parameters this tool accepts.
    /// </summary>
    string ParameterSchema { get; }

    /// <summary>
    /// Executes the tool with the given parameters.
    /// </summary>
    Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters);
}

/// <summary>
/// Result of a tool execution.
/// </summary>
public sealed class ToolResult
{
    public bool Success { get; init; }
    public string Output { get; init; } = string.Empty;
    public string? Error { get; init; }
    public Dictionary<string, object> Data { get; init; } = new();

    public static ToolResult Ok(string output) => new() { Success = true, Output = output };
    public static ToolResult Ok(object data) => new() { Success = true, Output = System.Text.Json.JsonSerializer.Serialize(data), Data = data is Dictionary<string, object> d ? d : new() };
    public static ToolResult Fail(string error) => new() { Success = false, Error = error };
}
