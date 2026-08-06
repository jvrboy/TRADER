using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Blends momentum indicators (ROC, RSI, and a moving-average gradient) into a
/// single momentum score in [-100, 100] with a label. Provides a quick read on
/// whether momentum is accelerating up or down.
/// </summary>
public sealed class MomentumTool : ITool
{
    public string Name => "analysis.momentum";
    public string Description => "Composite momentum score (ROC + RSI + trend gradient).";
    public string Parameters => "symbol, rocPeriod=12";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? context.Market.FirstOrDefault()?.Symbol ?? "?";
        var rocPeriod = int.TryParse(args.GetValueOrDefault("rocPeriod"), out var r) && r > 1 ? r : 12;

        var closes = context.Market.Where(c => c.Symbol == symbol).OrderBy(c => c.EpochSec).Select(c => c.Close).ToArray();
        if (closes.Length < Math.Max(rocPeriod + 1, 30))
            return Task.FromResult(ToolResult.Fail($"Not enough data for '{symbol}'."));

        // ROC
        var roc = (closes[^1] / closes[closes.Length - 1 - rocPeriod] - 1.0) * 100.0;

        // RSI (14)
        var rsi = Rsi(closes, 14);

        // Trend gradient: slope of last 10 closes normalized
        var n = Math.Min(10, closes.Length);
        var grad = LinearSlope(closes, n);

        // Composite score in [-100, 100]
        var score = Math.Clamp(roc * 2.0 + (rsi - 50) * 1.2 + grad * 200, -100, 100);

        var label = score switch
        {
            > 40 => "strong-bullish",
            > 10 => "mild-bullish",
            < -40 => "strong-bearish",
            < -10 => "mild-bearish",
            _ => "neutral"
        };

        var data = new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["rocPct"] = Math.Round(roc, 2),
            ["rsi"] = Math.Round(rsi, 2),
            ["gradient"] = Math.Round(grad, 6),
            ["score"] = Math.Round(score, 1),
            ["label"] = label,
        };

        var message = $"{symbol}: momentum {label} (score {data["score"]}), ROC {roc:0.00}%, RSI {rsi:0.0}.";
        return Task.FromResult(ToolResult.Ok(message, data));
    }

    private static double Rsi(double[] closes, int period)
    {
        if (closes.Length < period + 1) return 50;
        double gain = 0, loss = 0;
        for (var i = closes.Length - period; i < closes.Length - 1; i++)
        {
            var d = closes[i + 1] - closes[i];
            if (d >= 0) gain += d; else loss -= d;
        }
        if (loss == 0) return 100;
        return 100 - 100 / (1 + (gain / period) / (loss / period));
    }

    private static double LinearSlope(double[] values, int n)
    {
        var start = values.Length - n;
        double sx = 0, sy = 0, sxy = 0, sxx = 0;
        for (var i = 0; i < n; i++)
        {
            var x = (double)i;
            var y = values[start + i];
            sx += x; sy += y; sxy += x * y; sxx += x * x;
        }
        var denom = n * sxx - sx * sx;
        return denom == 0 ? 0 : (n * sxy - sx * sy) / denom;
    }
}
