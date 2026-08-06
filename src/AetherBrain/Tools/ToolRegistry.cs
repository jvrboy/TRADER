namespace AetherBrain.Tools;

public sealed class ToolRegistry
{
    private readonly Dictionary<string, Func<string, CancellationToken, Task<string>>> _tools = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string name, Func<string, CancellationToken, Task<string>> tool) => _tools[name] = tool;

    public Task<string> InvokeAsync(string name, string input, CancellationToken cancellationToken = default) =>
        _tools.TryGetValue(name, out var tool)
            ? tool(input, cancellationToken)
            : Task.FromResult($"Unknown tool: {name}");

    public IReadOnlyCollection<string> Names => _tools.Keys;
}
