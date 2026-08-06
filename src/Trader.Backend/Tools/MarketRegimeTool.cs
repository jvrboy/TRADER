using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Classifies the current market regime from the price series (trending vs
/// ranging, volatility level) — useful context for other tools and agents.
/// </summary>
public sealed class MarketRegimeTool : ITool
{
    public string Name => "market.regime";
    public string Description => "Classify trend and volatility regime from candles.";
    public string Parameters => "symbol";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? context.Market.FirstOrDefault()?.Symbol ?? "?";
        var candles = context.Market.Where(c => c.Symbol == symbol).OrderBy(c => c.EpochSec).ToArray();
        if (candles.Length < 30)
            return Task.FromResult(ToolResult.Fail($"Not enough data for '{symbol}'."));

        var closes = candles.Select(c => c.Close).ToArray();
        var returns = new double[closes.Length - 1];
        for (var i = 0; i < returns.Length; i++)
            returns[i] = (closes[i + 1] / closes[i]) - 1.0;

        var mean = returns.Average();
        var std = StdDev(returns, mean);
        var annualizedVol = std * Math.Sqrt(returns.Length); // rough proxy
        var trend = Math.Abs(mean) * closes.Length; // net drift over window

        var trendLabel = trend > 0.05 ? "trending-up" : trend < -0.05 ? "trending-down" : "ranging";
        var volLabel = annualizedVol switch
        {
            > 0.15 => "high-volatility",
            < 0.06 => "low-volatility",
            _ => "normal-volatility"
        };

        var data = new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["trend"] = trendLabel,
            ["volatility"] = volLabel,
            ["annualizedVolPct"] = Math.Round(annualizedVol * 100.0, 1),
            ["netDriftPct"] = Math.Round(trend * 100.0, 2),
            ["candles"] = closes.Length,
        };

        var message = $"{symbol}: {trendLabel}, {volLabel} ({data["annualizedVolPct"]}% vol).";
        return Task.FromResult(ToolResult.Ok(message, data));
    }

    private static double StdDev(double[] values, double mean)
    {
        if (values.Length < 2) return 0;
        var sum = values.Sum(v => (v - mean) * (v - mean));
        return Math.Sqrt(sum / (values.Length - 1));
    }
}
