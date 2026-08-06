using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Sector rotation analysis. Given a set of sector symbols with their price
/// series, ranks them by relative strength (momentum) to identify which sectors
/// are leading and lagging. Requires multiple symbols in the market context.
/// </summary>
public sealed class SectorRotationTool : ITool
{
    public string Name => "analysis.sector";
    public string Description => "Rank sectors by relative strength.";
    public string Parameters => "lookback=20";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var lookback = int.TryParse(args.GetValueOrDefault("lookback"), out var lb) && lb > 2 ? lb : 20;

        var sectors = context.Market
            .GroupBy(c => c.Symbol)
            .Select(g => new { Symbol = g.Key, Closes = g.OrderBy(c => c.EpochSec).Select(c => c.Close).ToArray() })
            .Where(s => s.Closes.Length > lookback)
            .Select(s => new
            {
                s.Symbol,
                Momentum = (s.Closes[^1] / s.Closes[s.Closes.Length - 1 - lookback] - 1.0) * 100.0,
            })
            .OrderByDescending(x => x.Momentum)
            .ToList();

        if (sectors.Count < 2)
            return Task.FromResult(ToolResult.Fail("Need at least two symbols with enough data to rank sectors."));

        var ranked = sectors
            .Select((s, i) => new Dictionary<string, object>
            {
                ["rank"] = i + 1,
                ["symbol"] = s.Symbol,
                ["momentumPct"] = Math.Round(s.Momentum, 2),
            })
            .ToList();

        var data = new Dictionary<string, object>
        {
            ["leader"] = sectors[0].Symbol,
            ["laggard"] = sectors[^1].Symbol,
            ["spreadPct"] = Math.Round(sectors[0].Momentum - sectors[^1].Momentum, 2),
            ["ranked"] = ranked,
        };

        var message = $"Sector rotation: leader {sectors[0].Symbol} ({sectors[0].Momentum:0.00}%), laggard {sectors[^1].Symbol} ({sectors[^1].Momentum:0.00}%).";
        return Task.FromResult(ToolResult.Ok(message, data));
    }
}
