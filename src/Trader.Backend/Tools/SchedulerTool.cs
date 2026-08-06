using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// A declarative scheduler that tells an orchestrator which tools to run on a
/// cadence. It does not execute anything itself — it returns a plan the caller
/// can execute, which keeps it simple and testable.
/// </summary>
public sealed class SchedulerTool : ITool
{
    public string Name => "scheduler.plan";
    public string Description => "Return a recommended run cadence for the given tools.";
    public string Parameters => "tools=csv, intervalMin=60";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var tools = (args.GetValueOrDefault("tools") ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        var interval = int.TryParse(args.GetValueOrDefault("intervalMin"), out var i) && i > 0 ? i : 60;

        var data = new Dictionary<string, object>
        {
            ["intervalMin"] = interval,
            ["tools"] = tools,
            ["nextRunUtc"] = context.Now.AddMinutes(interval).ToString("O"),
        };

        var message = tools.Count == 0
            ? "No tools scheduled. Pass 'tools' as a comma-separated list."
            : $"Schedule: run [{string.Join(", ", tools)}] every {interval} min. Next run {data["nextRunUtc"]}.";
        return Task.FromResult(ToolResult.Ok(message, data));
    }
}
