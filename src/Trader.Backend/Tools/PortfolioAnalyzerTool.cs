using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Summarizes the current portfolio: per-position P&L, total value, and the
/// best/worst performers.
/// </summary>
public sealed class PortfolioAnalyzerTool : ITool
{
    public string Name => "portfolio.summary";
    public string Description => "Summarize portfolio positions and P&L.";
    public string Parameters => "";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var positions = context.Portfolio.Positions;
        if (positions.Count == 0)
            return Task.FromResult(ToolResult.Ok("Portfolio is empty.", new Dictionary<string, object> { ["positions"] = 0 }));

        var rows = positions
            .OrderByDescending(p => p.PnlPct)
            .Select(p => $"{p.Symbol}: {p.Quantity:0.####} @ {p.EntryPrice:0.00} -> {p.CurrentPrice:0.00} ({p.PnlPct:+0.0;-0.0}%)")
            .ToList();

        var data = new Dictionary<string, object>
        {
            ["positions"] = positions.Count,
            ["totalValue"] = Math.Round(context.Portfolio.TotalValue, 2),
            ["unrealizedPnl"] = Math.Round(context.Portfolio.UnrealizedPnl, 2),
            ["best"] = positions.OrderByDescending(p => p.PnlPct).First().Symbol,
            ["worst"] = positions.OrderByDescending(p => p.PnlPct).Last().Symbol,
            ["rows"] = rows,
        };

        var message = $"{positions.Count} positions, value {context.Portfolio.TotalValue:0.00}, P&L {context.Portfolio.UnrealizedPnl:+0.00;-0.00}.";
        return Task.FromResult(ToolResult.Ok(message, data));
    }
}
