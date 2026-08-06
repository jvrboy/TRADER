using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Drawdown analysis: computes the maximum drawdown from an equity-like series
/// (compounded returns) and the current drawdown. Useful for evaluating a
/// strategy's risk profile.
/// </summary>
public sealed class DrawdownTool : ITool
{
    public string Name => "analysis.drawdown";
    public string Description => "Max drawdown and current drawdown from a price series.";
    public string Parameters => "symbol";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? context.Market.FirstOrDefault()?.Symbol ?? "?";
        var closes = context.Market.Where(c => c.Symbol == symbol).OrderBy(c => c.EpochSec).Select(c => c.Close).ToArray();
        if (closes.Length < 20)
            return Task.FromResult(ToolResult.Fail($"Not enough data for '{symbol}'."));

        double peak = closes[0];
        double maxDd = 0;
        double maxDdStart = closes[0], maxDdEnd = closes[0];
        for (var i = 1; i < closes.Length; i++)
        {
            if (closes[i] > peak)
            {
                peak = closes[i];
                maxDdStart = peak;
            }
            var dd = (peak - closes[i]) / peak;
            if (dd > maxDd)
            {
                maxDd = dd;
                maxDdEnd = closes[i];
                maxDdStart = peak;
            }
        }

        var currentDd = (peak - closes[^1]) / peak;

        var data = new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["maxDrawdownPct"] = Math.Round(maxDd * 100.0, 2),
            ["currentDrawdownPct"] = Math.Round(currentDd * 100.0, 2),
            ["peak"] = Math.Round(peak, 4),
            ["maxDdFrom"] = Math.Round(maxDdStart, 4),
            ["maxDdTo"] = Math.Round(maxDdEnd, 4),
            ["lastClose"] = Math.Round(closes[^1], 4),
        };

        var message = $"{symbol}: max drawdown {maxDd * 100:0.00}%, current drawdown {currentDd * 100:0.00}%.";
        return Task.FromResult(ToolResult.Ok(message, data));
    }
}
