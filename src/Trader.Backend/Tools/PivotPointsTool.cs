using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Calculates standard Classic, Fibonacci, Camarilla, and Woodie Pivot Points
/// and identifies immediate intraday support/resistance boundaries.
/// </summary>
public sealed class PivotPointsTool : ITool
{
    public string Name => "analysis.pivots";
    public string Description => "Calculates Classic, Fibonacci, Camarilla, and Woodie pivot levels.";

    public IReadOnlyList<ToolParam> Parameters => new[]
    {
        new ToolParam("symbol", "Ticker symbol to analyze", Required: true),
        new ToolParam("type", "Pivot method: 'classic', 'fibonacci', 'camarilla', 'woodie' (default: 'classic')", Required: false),
    };

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        if (!args.TryGetValue("symbol", out var symbol) || string.IsNullOrWhiteSpace(symbol))
            return Task.FromResult(ToolResult.Fail("Missing required 'symbol' parameter."));

        var type = (args.GetValueOrDefault("type") ?? "classic").ToLowerInvariant();
        var series = context.GetSeries(symbol);
        if (series.Length < 5)
            return Task.FromResult(ToolResult.Fail($"Insufficient data for pivot calculation. Need at least 5 candles, got {series.Length}."));

        // Use previous bar (or session) for High, Low, Close
        var prev = series[^2];
        var curr = series[^1].Close;
        var h = prev.High;
        var l = prev.Low;
        var c = prev.Close;
        var o = prev.Open;
        var range = h - l;

        var levels = new Dictionary<string, double>();
        double pivot;

        switch (type)
        {
            case "camarilla":
                pivot = c;
                var h4 = c + range * 1.1 / 2.0;
                var h3 = c + range * 1.1 / 4.0;
                var h2 = c + range * 1.1 / 6.0;
                var h1 = c + range * 1.1 / 12.0;
                var l1 = c - range * 1.1 / 12.0;
                var l2 = c - range * 1.1 / 6.0;
                var l3 = c - range * 1.1 / 4.0;
                var l4 = c - range * 1.1 / 2.0;
                levels["H4 (Breakout Long)"] = Math.Round(h4, 5);
                levels["H3 (Reversal Short)"] = Math.Round(h3, 5);
                levels["H2"] = Math.Round(h2, 5);
                levels["H1"] = Math.Round(h1, 5);
                levels["Pivot"] = Math.Round(pivot, 5);
                levels["L1"] = Math.Round(l1, 5);
                levels["L2"] = Math.Round(l2, 5);
                levels["L3 (Reversal Long)"] = Math.Round(l3, 5);
                levels["L4 (Breakout Short)"] = Math.Round(l4, 5);
                break;

            case "fibonacci":
                pivot = (h + l + c) / 3.0;
                levels["R3"] = Math.Round(pivot + range * 1.000, 5);
                levels["R2"] = Math.Round(pivot + range * 0.618, 5);
                levels["R1"] = Math.Round(pivot + range * 0.382, 5);
                levels["Pivot"] = Math.Round(pivot, 5);
                levels["S1"] = Math.Round(pivot - range * 0.382, 5);
                levels["S2"] = Math.Round(pivot - range * 0.618, 5);
                levels["S3"] = Math.Round(pivot - range * 1.000, 5);
                break;

            case "woodie":
                pivot = (h + l + 2 * o) / 4.0;
                levels["R2"] = Math.Round(pivot + range, 5);
                levels["R1"] = Math.Round(2 * pivot - l, 5);
                levels["Pivot"] = Math.Round(pivot, 5);
                levels["S1"] = Math.Round(2 * pivot - h, 5);
                levels["S2"] = Math.Round(pivot - range, 5);
                break;

            case "classic":
            default:
                pivot = (h + l + c) / 3.0;
                levels["R3"] = Math.Round(h + 2 * (pivot - l), 5);
                levels["R2"] = Math.Round(pivot + (h - l), 5);
                levels["R1"] = Math.Round(2 * pivot - l, 5);
                levels["Pivot"] = Math.Round(pivot, 5);
                levels["S1"] = Math.Round(2 * pivot - h, 5);
                levels["S2"] = Math.Round(pivot - (h - l), 5);
                levels["S3"] = Math.Round(l - 2 * (h - pivot), 5);
                break;
        }

        var bias = curr >= pivot ? "Bullish (Above Pivot)" : "Bearish (Below Pivot)";

        return Task.FromResult(ToolResult.Ok(
            $"{symbol} {type.ToUpperInvariant()} Pivots: Pivot {pivot:0.0000}, Current {curr:0.0000} ({bias}).",
            new Dictionary<string, object>
            {
                ["symbol"] = symbol,
                ["type"] = type,
                ["bias"] = bias,
                ["currentPrice"] = Math.Round(curr, 5),
                ["pivot"] = Math.Round(pivot, 5),
                ["levels"] = levels
            }));
    }
}
