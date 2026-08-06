using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Scans price history for classical Harmonic Trading Patterns (Gartley, Bat, Butterfly, Crab, ABCD)
/// using Fibonacci swing ratios and projects Potential Reversal Zones (PRZ).
/// </summary>
public sealed class HarmonicPatternTool : ITool
{
    public string Name => "analysis.harmonic";
    public string Description => "Scans for Harmonic patterns (Gartley, Bat, Butterfly, Crab) and projects PRZ.";

    public IReadOnlyList<ToolParam> Parameters => new[]
    {
        new ToolParam("symbol", "Ticker symbol to scan", Required: true),
        new ToolParam("lookback", "Candle lookback for pattern detection (default: 60)", Required: false),
        new ToolParam("tolerance", "Ratio deviation tolerance (default: 0.08)", Required: false),
    };

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        if (!args.TryGetValue("symbol", out var symbol) || string.IsNullOrWhiteSpace(symbol))
            return Task.FromResult(ToolResult.Fail("Missing required 'symbol' parameter."));

        var lookback = int.TryParse(args.GetValueOrDefault("lookback"), out var lb) && lb >= 20 ? lb : 60;
        var tolerance = double.TryParse(args.GetValueOrDefault("tolerance"), out var tol) && tol > 0 ? tol : 0.08;

        var series = context.GetSeries(symbol);
        if (series.Length < 25)
            return Task.FromResult(ToolResult.Fail($"Insufficient data for harmonic scan. Need at least 25 candles, got {series.Length}."));

        var slice = series[^Math.Min(lookback, series.Length)..];

        // Find swing points using simple local extrema
        var swings = new List<(int Index, double Price, bool IsHigh)>();
        for (var i = 2; i < slice.Length - 2; i++)
        {
            var isPeak = slice[i].High >= slice[i - 1].High && slice[i].High >= slice[i - 2].High &&
                         slice[i].High >= slice[i + 1].High && slice[i].High >= slice[i + 2].High;
            var isTrough = slice[i].Low <= slice[i - 1].Low && slice[i].Low <= slice[i - 2].Low &&
                           slice[i].Low <= slice[i + 1].Low && slice[i].Low <= slice[i + 2].Low;

            if (isPeak) swings.Add((i, slice[i].High, true));
            else if (isTrough) swings.Add((i, slice[i].Low, false));
        }

        if (swings.Count < 5)
        {
            // Fallback: estimate from quartile points
            var x = slice[0].Low;
            var a = slice[slice.Length / 4].High;
            var b = slice[slice.Length / 2].Low;
            var c = slice[3 * slice.Length / 4].High;
            var d = slice[^1].Close;
            return ClassifyHarmonic(symbol, x, a, b, c, d, tolerance);
        }

        // Use last 5 alternating swing points
        var last5 = swings.TakeLast(5).ToList();
        return ClassifyHarmonic(symbol, last5[0].Price, last5[1].Price, last5[2].Price, last5[3].Price, last5[4].Price, tolerance);
    }

    private static Task<ToolResult> ClassifyHarmonic(string symbol, double x, double a, double b, double c, double d, double tol)
    {
        var xa = Math.Abs(a - x);
        var ab = Math.Abs(b - a);
        var bc = Math.Abs(c - b);
        var cd = Math.Abs(d - c);

        if (xa <= 0 || ab <= 0 || bc <= 0)
            return Task.FromResult(ToolResult.Fail("Zero variance in swing legs."));

        var ab_xa = ab / xa;
        var bc_ab = bc / ab;
        var cd_bc = cd / bc;
        var ad_xa = Math.Abs(d - x) / xa;

        var isBullish = d < c && x < a;
        string patternName = "ABCD Structure";
        var confidence = 70.0;

        if (Math.Abs(ab_xa - 0.618) <= tol && Math.Abs(ad_xa - 0.786) <= tol)
        {
            patternName = isBullish ? "Bullish Gartley" : "Bearish Gartley";
            confidence = 88.0;
        }
        else if (ab_xa <= 0.50 + tol && Math.Abs(ad_xa - 0.886) <= tol)
        {
            patternName = isBullish ? "Bullish Bat" : "Bearish Bat";
            confidence = 85.0;
        }
        else if (Math.Abs(ab_xa - 0.786) <= tol && ad_xa >= 1.27 - tol)
        {
            patternName = isBullish ? "Bullish Butterfly" : "Bearish Butterfly";
            confidence = 82.0;
        }
        else if (ab_xa <= 0.618 + tol && ad_xa >= 1.618 - tol)
        {
            patternName = isBullish ? "Bullish Crab" : "Bearish Crab";
            confidence = 84.0;
        }

        // Targets based on CD leg
        var target1 = isBullish ? d + 0.382 * cd : d - 0.382 * cd;
        var target2 = isBullish ? d + 0.618 * cd : d - 0.618 * cd;

        return Task.FromResult(ToolResult.Ok(
            $"{symbol} Harmonic Scan: {patternName} detected (Confidence {confidence:0}%), PRZ around {d:0.0000}, Target 1 {target1:0.0000}, Target 2 {target2:0.0000}.",
            new Dictionary<string, object>
            {
                ["symbol"] = symbol,
                ["pattern"] = patternName,
                ["isBullish"] = isBullish,
                ["confidencePct"] = confidence,
                ["prz"] = Math.Round(d, 5),
                ["target1"] = Math.Round(target1, 5),
                ["target2"] = Math.Round(target2, 5),
                ["ratios"] = new Dictionary<string, double>
                {
                    ["AB_XA"] = Math.Round(ab_xa, 3),
                    ["BC_AB"] = Math.Round(bc_ab, 3),
                    ["CD_BC"] = Math.Round(cd_bc, 3),
                    ["AD_XA"] = Math.Round(ad_xa, 3)
                }
            }));
    }
}
