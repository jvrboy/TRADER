using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Risk-adjusted return metrics: Sharpe and Sortino ratios computed from a
/// price series. Uses a simple annualization factor (periods per year) so it
/// works for any candle frequency.
/// </summary>
public sealed class SharpeSortinoTool : ITool
{
    public string Name => "analysis.riskmetrics";
    public string Description => "Sharpe and Sortino ratios from returns.";
    public string Parameters => "symbol, periodsPerYear=252, rf=0";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? context.Market.FirstOrDefault()?.Symbol ?? "?";
        var ppy = int.TryParse(args.GetValueOrDefault("periodsPerYear"), out var p) && p > 0 ? p : 252;
        var rf = double.TryParse(args.GetValueOrDefault("rf"), out var r) ? r : 0.0;

        var closes = context.Market.Where(c => c.Symbol == symbol).OrderBy(c => c.EpochSec).Select(c => c.Close).ToArray();
        if (closes.Length < 20)
            return Task.FromResult(ToolResult.Fail($"Not enough data for '{symbol}'."));

        var returns = new double[closes.Length - 1];
        for (var i = 0; i < returns.Length; i++)
            returns[i] = (closes[i + 1] / closes[i]) - 1.0;

        var mean = returns.Average();
        var rfPer = rf / ppy;
        var excess = mean - rfPer;

        var totalStd = StdDev(returns, mean);
        var downside = returns.Where(r => r < rfPer).ToArray();
        var downsideStd = downside.Length == 0 ? 0 : StdDev(downside, downside.Average());

        var sharpe = totalStd == 0 ? 0 : excess / totalStd * Math.Sqrt(ppy);
        var sortino = downsideStd == 0 ? 0 : excess / downsideStd * Math.Sqrt(ppy);

        var data = new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["sharpe"] = Math.Round(sharpe, 2),
            ["sortino"] = Math.Round(sortino, 2),
            ["annualizedReturnPct"] = Math.Round(mean * ppy * 100.0, 2),
            ["annualizedVolPct"] = Math.Round(totalStd * Math.Sqrt(ppy) * 100.0, 2),
            ["periodsPerYear"] = ppy,
        };

        var message = $"{symbol}: Sharpe {sharpe:0.00}, Sortino {sortino:0.00}.";
        return Task.FromResult(ToolResult.Ok(message, data));
    }

    private static double StdDev(double[] values, double mean)
    {
        if (values.Length < 2) return 0;
        return Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / (values.Length - 1));
    }
}
