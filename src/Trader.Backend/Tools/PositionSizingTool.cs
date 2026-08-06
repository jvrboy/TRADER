using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Computes optimal position sizing using Fixed-Risk, Full Kelly, Half Kelly, or Volatility-Adjusted models.
/// </summary>
public sealed class PositionSizingTool : ITool
{
    public string Name => "risk.positionsize";
    public string Description => "Calculates optimal position sizing using Fixed-Risk, Kelly, and Half-Kelly criterion.";

    public IReadOnlyList<ToolParam> Parameters => new[]
    {
        new ToolParam("accountEquity", "Total account balance/equity", Required: true),
        new ToolParam("entry", "Trade entry price", Required: true),
        new ToolParam("stopLoss", "Trade stop loss price", Required: true),
        new ToolParam("riskPct", "Max risk per trade as % of equity (default: 1.0)", Required: false),
        new ToolParam("winRate", "Historical strategy win rate [0..1] (default: 0.55)", Required: false),
        new ToolParam("payoffRatio", "Win/Loss payoff ratio (default: 1.8)", Required: false),
        new ToolParam("model", "Sizing model: 'fixed', 'kelly', 'half-kelly' (default: 'half-kelly')", Required: false),
    };

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        if (!double.TryParse(args.GetValueOrDefault("accountEquity"), out var equity) || equity <= 0)
            return Task.FromResult(ToolResult.Fail("Invalid or missing 'accountEquity'. Must be > 0."));

        if (!double.TryParse(args.GetValueOrDefault("entry"), out var entry) || entry <= 0)
            return Task.FromResult(ToolResult.Fail("Invalid or missing 'entry' price."));

        if (!double.TryParse(args.GetValueOrDefault("stopLoss"), out var stopLoss) || stopLoss <= 0)
            return Task.FromResult(ToolResult.Fail("Invalid or missing 'stopLoss' price."));

        var riskPerUnit = Math.Abs(entry - stopLoss);
        if (riskPerUnit <= 0)
            return Task.FromResult(ToolResult.Fail("Entry and StopLoss cannot be identical."));

        var riskPct = double.TryParse(args.GetValueOrDefault("riskPct"), out var rp) && rp > 0 ? rp : 1.0;
        var winRate = double.TryParse(args.GetValueOrDefault("winRate"), out var wr) && wr > 0 && wr < 1 ? wr : 0.55;
        var payoffRatio = double.TryParse(args.GetValueOrDefault("payoffRatio"), out var pr) && pr > 0 ? pr : 1.8;
        var model = (args.GetValueOrDefault("model") ?? "half-kelly").ToLowerInvariant();

        // 1. Fixed Fractional Risk Budget
        var fixedBudget = equity * (riskPct / 100.0);

        // 2. Kelly Criterion: K = W - (1 - W) / R
        var fullKellyFraction = winRate - (1.0 - winRate) / payoffRatio;
        var halfKellyFraction = Math.Max(0.0, fullKellyFraction / 2.0);

        // Cap Kelly fraction at realistic risk boundaries (max 5%)
        var effectiveKellyPct = Math.Clamp(halfKellyFraction * 100.0, 0.25, 5.0);
        var kellyBudget = equity * (effectiveKellyPct / 100.0);

        var finalBudget = model switch
        {
            "kelly" => equity * (Math.Clamp(fullKellyFraction * 100.0, 0.25, 5.0) / 100.0),
            "fixed" => fixedBudget,
            _ => kellyBudget
        };

        var units = Math.Floor(finalBudget / riskPerUnit);
        var capitalExposure = units * entry;
        var maxLoss = units * riskPerUnit;
        var leverageRequired = Math.Round(capitalExposure / equity, 2);

        return Task.FromResult(ToolResult.Ok(
            $"Position Size ({model}): {units:N0} units (${capitalExposure:N2} exposure, {leverageRequired}x leverage). Max Risk: ${maxLoss:N2} ({maxLoss / equity * 100.0:0.00}% of equity).",
            new Dictionary<string, object>
            {
                ["accountEquity"] = equity,
                ["entry"] = entry,
                ["stopLoss"] = stopLoss,
                ["riskPerUnit"] = Math.Round(riskPerUnit, 5),
                ["model"] = model,
                ["units"] = units,
                ["capitalExposure"] = Math.Round(capitalExposure, 2),
                ["maxRiskDollars"] = Math.Round(maxLoss, 2),
                ["actualRiskPct"] = Math.Round(maxLoss / equity * 100.0, 2),
                ["leverageRatio"] = leverageRequired,
                ["fullKellyFraction"] = Math.Round(fullKellyFraction, 4),
                ["halfKellyFraction"] = Math.Round(halfKellyFraction, 4),
            }));
    }
}
