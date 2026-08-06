using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Computes the risk/reward ratio for a proposed trade given an entry, stop
/// loss and take profit, and grades whether the setup is worth taking.
/// </summary>
public sealed class RiskRewardTool : ITool
{
    public string Name => "analysis.riskreward";
    public string Description => "Risk/reward ratio and quality grade for a trade.";
    public string Parameters => "entry, stopLoss, takeProfit, direction=buy";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        if (!double.TryParse(args.GetValueOrDefault("entry"), out var entry) || entry <= 0)
            return Task.FromResult(ToolResult.Fail("Provide a valid 'entry' price."));
        if (!double.TryParse(args.GetValueOrDefault("stopLoss"), out var sl) || sl <= 0)
            return Task.FromResult(ToolResult.Fail("Provide a valid 'stopLoss' price."));
        if (!double.TryParse(args.GetValueOrDefault("takeProfit"), out var tp) || tp <= 0)
            return Task.FromResult(ToolResult.Fail("Provide a valid 'takeProfit' price."));

        var direction = (args.GetValueOrDefault("direction") ?? "buy").ToLowerInvariant();

        double risk, reward;
        if (direction == "buy" || direction == "long")
        {
            if (tp <= entry || sl >= entry)
                return Task.FromResult(ToolResult.Fail("For a buy, takeProfit must be above entry and stopLoss below."));
            risk = entry - sl;
            reward = tp - entry;
        }
        else
        {
            if (tp >= entry || sl <= entry)
                return Task.FromResult(ToolResult.Fail("For a sell, takeProfit must be below entry and stopLoss above."));
            risk = sl - entry;
            reward = entry - tp;
        }

        var rr = risk <= 0 ? 0 : reward / risk;
        var grade = rr switch
        {
            >= 3.0 => "excellent",
            >= 2.0 => "good",
            >= 1.5 => "acceptable",
            >= 1.0 => "marginal",
            _ => "poor"
        };

        var data = new Dictionary<string, object>
        {
            ["entry"] = Math.Round(entry, 4),
            ["stopLoss"] = Math.Round(sl, 4),
            ["takeProfit"] = Math.Round(tp, 4),
            ["risk"] = Math.Round(risk, 4),
            ["reward"] = Math.Round(reward, 4),
            ["riskReward"] = Math.Round(rr, 2),
            ["grade"] = grade,
        };

        var message = $"R:R = {rr:0.00} ({grade}). Risk {risk:0.0000}, reward {reward:0.0000}.";
        return Task.FromResult(ToolResult.Ok(message, data));
    }
}
