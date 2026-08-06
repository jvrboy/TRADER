using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Performs Monte Carlo simulation of trading strategies across hundreds of equity paths
/// to compute risk of ruin, expected drawdown distribution, and terminal equity percentiles.
/// </summary>
public sealed class MonteCarloTool : ITool
{
    public string Name => "risk.montecarlo";
    public string Description => "Simulates hundreds of trading equity paths to compute Drawdown & Risk of Ruin.";

    public IReadOnlyList<ToolParam> Parameters => new[]
    {
        new ToolParam("startEquity", "Starting account balance (default: 100000)", Required: false),
        new ToolParam("tradesCount", "Number of trades per simulated path (default: 100)", Required: false),
        new ToolParam("winRate", "Strategy win rate [0..1] (default: 0.55)", Required: false),
        new ToolParam("winLossRatio", "Win/Loss payoff ratio (default: 1.5)", Required: false),
        new ToolParam("riskPct", "Risk percentage per trade (default: 1.0)", Required: false),
        new ToolParam("iterations", "Number of simulation iterations (default: 300)", Required: false),
    };

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var startEquity = double.TryParse(args.GetValueOrDefault("startEquity"), out var se) && se > 0 ? se : 100_000.0;
        var tradesCount = int.TryParse(args.GetValueOrDefault("tradesCount"), out var tc) && tc >= 10 ? tc : 100;
        var winRate = double.TryParse(args.GetValueOrDefault("winRate"), out var wr) && wr > 0 && wr < 1 ? wr : 0.55;
        var winLossRatio = double.TryParse(args.GetValueOrDefault("winLossRatio"), out var wlr) && wlr > 0 ? wlr : 1.5;
        var riskPct = double.TryParse(args.GetValueOrDefault("riskPct"), out var rp) && rp > 0 ? rp : 1.0;
        var iterations = int.TryParse(args.GetValueOrDefault("iterations"), out var it) && it >= 50 ? Math.Min(it, 1000) : 300;

        var endingEquities = new double[iterations];
        var maxDrawdowns = new double[iterations];
        var ruinedCount = 0;
        var rng = new Random(42); // Deterministic seed for reproducible results

        for (var i = 0; i < iterations; i++)
        {
            var equity = startEquity;
            var peak = equity;
            var maxDd = 0.0;

            for (var t = 0; t < tradesCount; t++)
            {
                var riskAmount = equity * (riskPct / 100.0);
                var isWin = rng.NextDouble() < winRate;

                if (isWin)
                    equity += riskAmount * winLossRatio;
                else
                    equity -= riskAmount;

                if (equity > peak)
                    peak = equity;

                var dd = (peak - equity) / peak * 100.0;
                if (dd > maxDd)
                    maxDd = dd;

                if (equity < startEquity * 0.5) // Ruin defined as 50% drawdown
                {
                    ruinedCount++;
                    break;
                }
            }

            endingEquities[i] = equity;
            maxDrawdowns[i] = maxDd;
        }

        Array.Sort(endingEquities);
        Array.Sort(maxDrawdowns);

        var p10 = endingEquities[(int)(iterations * 0.10)];
        var p50 = endingEquities[(int)(iterations * 0.50)];
        var p90 = endingEquities[(int)(iterations * 0.90)];
        var meanDd = maxDrawdowns.Average();
        var p95Dd = maxDrawdowns[(int)(iterations * 0.95)];
        var ruinProb = Math.Round((double)ruinedCount / iterations * 100.0, 2);

        return Task.FromResult(ToolResult.Ok(
            $"Monte Carlo ({iterations} runs x {tradesCount} trades): Median ${p50:N0} (10th: ${p10:N0}, 90th: ${p90:N0}), Avg MaxDD: {meanDd:0.1}%, 95th MaxDD: {p95Dd:0.1}%, Ruin Prob: {ruinProb}%.",
            new Dictionary<string, object>
            {
                ["startEquity"] = startEquity,
                ["tradesCount"] = tradesCount,
                ["iterations"] = iterations,
                ["medianEndingEquity"] = Math.Round(p50, 2),
                ["percentile10Equity"] = Math.Round(p10, 2),
                ["percentile90Equity"] = Math.Round(p90, 2),
                ["meanMaxDrawdownPct"] = Math.Round(meanDd, 2),
                ["percentile95MaxDrawdownPct"] = Math.Round(p95Dd, 2),
                ["ruinProbabilityPct"] = ruinProb
            }));
    }
}
