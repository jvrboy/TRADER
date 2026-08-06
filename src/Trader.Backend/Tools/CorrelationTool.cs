using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Computes the Pearson correlation between two symbols' return series. Useful
/// for diversification and hedging analysis. Pure computation.
/// </summary>
public sealed class CorrelationTool : ITool
{
    public string Name => "analysis.correlation";
    public string Description => "Pearson correlation between two symbols' returns.";
    public string Parameters => "symbolA, symbolB, lookback=50";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var a = args.GetValueOrDefault("symbolA") ?? context.Market.FirstOrDefault()?.Symbol ?? "?";
        var b = args.GetValueOrDefault("symbolB") ?? "";
        var lookback = int.TryParse(args.GetValueOrDefault("lookback"), out var lb) && lb > 2 ? lb : 50;

        if (string.IsNullOrEmpty(b))
            return Task.FromResult(ToolResult.Fail("Provide 'symbolB' to correlate against."));

        var closesA = context.Market.Where(c => c.Symbol == a).OrderBy(c => c.EpochSec).Select(c => c.Close).ToArray();
        var closesB = context.Market.Where(c => c.Symbol == b).OrderBy(c => c.EpochSec).Select(c => c.Close).ToArray();

        var n = Math.Min(lookback, Math.Min(closesA.Length, closesB.Length));
        if (n < 10)
            return Task.FromResult(ToolResult.Fail($"Not enough data to correlate '{a}' and '{b}'."));

        // Align to the last n closes of each series.
        var ra = Returns(closesA, n);
        var rb = Returns(closesB, n);

        var corr = Pearson(ra, rb);
        var label = corr switch
        {
            > 0.7 => "strongly correlated",
            > 0.3 => "moderately correlated",
            < -0.7 => "strongly inversely correlated",
            < -0.3 => "moderately inversely correlated",
            _ => "weakly correlated"
        };

        var data = new Dictionary<string, object>
        {
            ["symbolA"] = a,
            ["symbolB"] = b,
            ["correlation"] = Math.Round(corr, 3),
            ["label"] = label,
            ["samples"] = n,
        };

        var message = $"{a} vs {b}: r = {corr:0.000} ({label}).";
        return Task.FromResult(ToolResult.Ok(message, data));
    }

    private static double[] Returns(double[] closes, int n)
    {
        var rets = new double[n - 1];
        var start = closes.Length - n;
        for (var i = 0; i < n - 1; i++)
            rets[i] = (closes[start + i + 1] / closes[start + i]) - 1.0;
        return rets;
    }

    private static double Pearson(double[] x, double[] y)
    {
        var n = Math.Min(x.Length, y.Length);
        if (n < 2) return 0;
        double mx = 0, my = 0;
        for (var i = 0; i < n; i++) { mx += x[i]; my += y[i]; }
        mx /= n; my /= n;
        double num = 0, dx2 = 0, dy2 = 0;
        for (var i = 0; i < n; i++)
        {
            var dx = x[i] - mx;
            var dy = y[i] - my;
            num += dx * dy;
            dx2 += dx * dx;
            dy2 += dy * dy;
        }
        if (dx2 == 0 || dy2 == 0) return 0;
        return num / Math.Sqrt(dx2 * dy2);
    }
}
