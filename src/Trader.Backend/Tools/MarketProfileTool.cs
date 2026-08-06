using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Market profile / volume profile analysis. Buckets volume by price level and
/// computes the Point of Control (POC), Value Area High/Low (VAH/VAL) and the
/// value area (the price range containing ~70% of volume). Useful for
/// identifying institutional price acceptance and high-volume nodes.
/// </summary>
public sealed class MarketProfileTool : ITool
{
    public string Name => "analysis.marketprofile";
    public string Description => "Volume profile: POC, value area high/low.";
    public string Parameters => "symbol, buckets=30, valueAreaPct=70";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? context.Market.FirstOrDefault()?.Symbol ?? "?";
        var buckets = int.TryParse(args.GetValueOrDefault("buckets"), out var b) && b >= 10 ? b : 30;
        var valueAreaPct = double.TryParse(args.GetValueOrDefault("valueAreaPct"), out var va) && va > 0 && va < 100 ? va : 70;

        var candles = context.Market.Where(c => c.Symbol == symbol).ToArray();
        if (candles.Length < 20)
            return Task.FromResult(ToolResult.Fail($"Not enough data for '{symbol}'."));

        var min = candles.Min(c => c.Low);
        var max = candles.Max(c => c.High);
        if (max <= min)
            return Task.FromResult(ToolResult.Fail("Price range is empty."));

        var step = (max - min) / buckets;
        var volumeByBucket = new double[buckets];

        foreach (var c in candles)
        {
            // Distribute candle volume across the price range it spans.
            var lo = (int)Math.Floor((c.Low - min) / step);
            var hi = (int)Math.Floor((c.High - min) / step);
            lo = Math.Clamp(lo, 0, buckets - 1);
            hi = Math.Clamp(hi, lo, buckets - 1);
            var span = hi - lo + 1;
            var per = c.Volume / span;
            for (var i = lo; i <= hi; i++) volumeByBucket[i] += per;
        }

        // Point of Control: bucket with max volume
        var pocBucket = Array.IndexOf(volumeByBucket, volumeByBucket.Max());
        var poc = min + (pocBucket + 0.5) * step;

        // Value area: expand from POC until we capture ~valueAreaPct of volume
        var totalVol = volumeByBucket.Sum();
        var target = totalVol * valueAreaPct / 100.0;
        var captured = volumeByBucket[pocBucket];
        var loB = pocBucket;
        var hiB = pocBucket;
        while (captured < target && (loB > 0 || hiB < buckets - 1))
        {
            var nextLo = loB > 0 ? (loB - 1, volumeByBucket[loB - 1]) : (-1, 0);
            var nextHi = hiB < buckets - 1 ? (hiB + 1, volumeByBucket[hiB + 1]) : (-1, 0);
            if (nextLo.Item2 >= nextHi.Item2 && nextLo.Item1 >= 0)
            {
                loB = nextLo.Item1; captured += nextLo.Item2;
            }
            else if (nextHi.Item1 >= 0)
            {
                hiB = nextHi.Item1; captured += nextHi.Item2;
            }
            else break;
        }

        var vah = min + (hiB + 1) * step;
        var val = min + loB * step;
        var current = candles[^1].Close;

        var data = new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["poc"] = Math.Round(poc, 4),
            ["valueAreaHigh"] = Math.Round(vah, 4),
            ["valueAreaLow"] = Math.Round(val, 4),
            ["current"] = Math.Round(current, 4),
            ["valueAreaPct"] = valueAreaPct,
            ["inValueArea"] = current >= val && current <= vah,
        };

        var message = $"{symbol}: POC {poc:0.0000}, value area {val:0.0000}-{vah:0.0000}, current {current:0.0000}.";
        return Task.FromResult(ToolResult.Ok(message, data));
    }
}
