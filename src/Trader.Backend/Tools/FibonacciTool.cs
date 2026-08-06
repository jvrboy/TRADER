using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Calculates Fibonacci retracement and extension levels based on recent swing highs and lows,
/// identifying golden pocket zones and key reaction levels.
/// </summary>
public sealed class FibonacciTool : ITool
{
    public string Name => "analysis.fibonacci";
    public string Description => "Computes Fibonacci retracement and extension levels for swing high/low.";

    public IReadOnlyList<ToolParam> Parameters => new[]
    {
        new ToolParam("symbol", "Ticker symbol to analyze", Required: true),
        new ToolParam("lookback", "Candle lookback for swing detection (default: 50)", Required: false),
    };

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        if (!args.TryGetValue("symbol", out var symbol) || string.IsNullOrWhiteSpace(symbol))
            return Task.FromResult(ToolResult.Fail("Missing required 'symbol' parameter."));

        var lookback = int.TryParse(args.GetValueOrDefault("lookback"), out var lb) && lb >= 5 ? lb : 50;
        var series = context.GetSeries(symbol);
        if (series.Length < 10)
            return Task.FromResult(ToolResult.Fail($"Insufficient candle data for {symbol}. Need at least 10, got {series.Length}."));

        var slice = series[^Math.Min(lookback, series.Length)..];
        var high = slice.Max(c => c.High);
        var low = slice.Min(c => c.Low);
        var current = series[^1].Close;
        var range = high - low;

        if (range <= 0)
            return Task.FromResult(ToolResult.Fail("Invalid price range (zero variance)."));

        var highIndex = Array.FindLastIndex(slice, c => Math.Abs(c.High - high) < 1e-6);
        var lowIndex = Array.FindLastIndex(slice, c => Math.Abs(c.Low - low) < 1e-6);
        var isUptrend = lowIndex < highIndex;

        // Fibonacci Retracement Levels
        var r236 = isUptrend ? high - 0.236 * range : low + 0.236 * range;
        var r382 = isUptrend ? high - 0.382 * range : low + 0.382 * range;
        var r500 = isUptrend ? high - 0.500 * range : low + 0.500 * range;
        var r618 = isUptrend ? high - 0.618 * range : low + 0.618 * range;
        var r786 = isUptrend ? high - 0.786 * range : low + 0.786 * range;

        // Fibonacci Extension Levels
        var ext1272 = isUptrend ? low + 1.272 * range : high - 1.272 * range;
        var ext1618 = isUptrend ? low + 1.618 * range : high - 1.618 * range;
        var ext2000 = isUptrend ? low + 2.000 * range : high - 2.000 * range;

        // Proximity to golden pocket (0.618 - 0.65)
        var inGoldenPocket = isUptrend
            ? current <= r618 && current >= high - 0.65 * range
            : current >= r618 && current <= low + 0.65 * range;

        var levels = new Dictionary<string, double>
        {
            ["0.0%"] = isUptrend ? high : low,
            ["23.6%"] = Math.Round(r236, 5),
            ["38.2%"] = Math.Round(r382, 5),
            ["50.0%"] = Math.Round(r500, 5),
            ["61.8% (Golden)"] = Math.Round(r618, 5),
            ["78.6%"] = Math.Round(r786, 5),
            ["100.0%"] = isUptrend ? low : high,
            ["Ext 127.2%"] = Math.Round(ext1272, 5),
            ["Ext 161.8%"] = Math.Round(ext1618, 5),
            ["Ext 200.0%"] = Math.Round(ext2000, 5),
        };

        var closestLevel = levels.OrderBy(kv => Math.Abs(kv.Value - current)).First();
        var distancePct = Math.Round(Math.Abs(current - closestLevel.Value) / current * 100.0, 3);

        return Task.FromResult(ToolResult.Ok(
            $"{symbol} Fib ({(isUptrend ? "Uptrend" : "Downtrend")}): Current {current:0.0000}, nearest {closestLevel.Key} @ {closestLevel.Value:0.0000} ({distancePct}% away){(inGoldenPocket ? " [IN GOLDEN POCKET]" : "")}.",
            new Dictionary<string, object>
            {
                ["symbol"] = symbol,
                ["direction"] = isUptrend ? "uptrend" : "downtrend",
                ["swingHigh"] = high,
                ["swingLow"] = low,
                ["currentPrice"] = current,
                ["inGoldenPocket"] = inGoldenPocket,
                ["nearestLevel"] = closestLevel.Key,
                ["nearestLevelPrice"] = closestLevel.Value,
                ["distancePct"] = distancePct,
                ["levels"] = levels
            }));
    }
}
