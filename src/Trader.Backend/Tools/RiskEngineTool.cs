using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Assesses portfolio risk: concentration, unrealized P&L, and a suggested
/// position size using a fixed-fractional (risk-per-trade) model.
/// </summary>
public sealed class RiskEngineTool : ITool
{
    public string Name => "risk.assess";
    public string Description => "Assess portfolio risk and suggest position size.";
    public string Parameters => "accountEquity=100000, riskPct=1.0";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var equity = double.TryParse(args.GetValueOrDefault("accountEquity"), out var e) && e > 0 ? e : 100_000;
        var riskPct = double.TryParse(args.GetValueOrDefault("riskPct"), out var r) ? r : 1.0;

        var positions = context.Portfolio.Positions;
        var unrealized = context.Portfolio.UnrealizedPnl;
        var exposure = context.Portfolio.TotalValue;
        var exposurePct = equity <= 0 ? 0 : (exposure / equity) * 100.0;

        // Concentration: largest single position share of total value
        var largest = positions.OrderByDescending(p => p.Value).FirstOrDefault();
        var concentration = largest is not null && exposure > 0
            ? (largest.Value / exposure) * 100.0
            : 0.0;

        // Suggested risk budget in currency
        var riskBudget = equity * (riskPct / 100.0);

        var flags = new List<string>();
        if (exposurePct > 80) flags.Add("high exposure");
        if (concentration > 40) flags.Add("concentrated");
        if (positions.Any(p => p.PnlPct < -10)) flags.Add("drawdown");

        var data = new Dictionary<string, object>
        {
            ["equity"] = Math.Round(equity, 2),
            ["riskPct"] = riskPct,
            ["riskBudget"] = Math.Round(riskBudget, 2),
            ["totalValue"] = Math.Round(exposure, 2),
            ["exposurePct"] = Math.Round(exposurePct, 1),
            ["unrealizedPnl"] = Math.Round(unrealized, 2),
            ["largestPosition"] = largest?.Symbol ?? "none",
            ["concentrationPct"] = Math.Round(concentration, 1),
            ["flags"] = flags,
        };

        var message = flags.Count == 0
            ? $"Risk OK: exposure {exposurePct:0.0}%, P&L {unrealized:0.00}, risk budget {riskBudget:0.00}."
            : $"Risk flags: {string.Join(", ", flags)}. Exposure {exposurePct:0.0}%, risk budget {riskBudget:0.00}.";

        return Task.FromResult(ToolResult.Ok(message, data));
    }
}
