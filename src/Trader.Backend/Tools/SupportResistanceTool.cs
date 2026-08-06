using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Detects key support and resistance levels from swing pivots and reports the
/// nearest levels around the current price. Useful for entries and targets.
/// </summary>
public sealed class SupportResistanceTool : ITool
{
    public string Name => "analysis.supplydemand";
    public string Description => "Nearest support and resistance levels from pivots.";
    public string Parameters => "symbol, strength=3, maxLevels=4";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? context.Market.FirstOrDefault()?.Symbol ?? "?";
        var strength = int.TryParse(args.GetValueOrDefault("strength"), out var s) && s >= 1 ? s : 3;
        var maxLevels = int.TryParse(args.GetValueOrDefault("maxLevels"), out var m) && m >= 1 ? m : 4;

        var candles = context.Market.Where(c => c.Symbol == symbol).OrderBy(c => c.EpochSec).ToArray();
        if (candles.Length < 30)
            return Task.FromResult(ToolResult.Fail($"Not enough data for '{symbol}'."));

        var highs = candles.Select(c => c.High).ToArray();
        var lows = candles.Select(c => c.Low).ToArray();
        var closes = candles.Select(c => c.Close).ToArray();
        var current = closes[^1];

        var resistances = PivotHighs(highs, strength)
            .Select(i => highs[i])
            .Where(px => px > current)
            .Distinct()
            .OrderBy(px => px)
            .Take(maxLevels)
            .ToArray();

        var supports = PivotLows(lows, strength)
            .Select(i => lows[i])
            .Where(px => px < current)
            .Distinct()
            .OrderByDescending(px => px)
            .Take(maxLevels)
            .ToArray();

        var nearestResistance = resistances.FirstOrDefault();
        var nearestSupport = supports.FirstOrDefault();

        var data = new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["current"] = Math.Round(current, 4),
            ["supports"] = supports.Select(px => Math.Round(px, 4)).ToArray(),
            ["resistances"] = resistances.Select(px => Math.Round(px, 4)).ToArray(),
            ["nearestSupport"] = Math.Round(nearestSupport, 4),
            ["nearestResistance"] = Math.Round(nearestResistance, 4),
        };

        var message = $"{symbol} @ {current:0.0000}: support {nearestSupport:0.0000}, resistance {nearestResistance:0.0000}.";
        return Task.FromResult(ToolResult.Ok(message, data));
    }

    private static int[] PivotHighs(double[] a, int strength)
    {
        var res = new List<int>();
        for (var i = strength; i < a.Length - strength; i++)
        {
            var ok = true;
            for (var k = 1; k <= strength; k++)
                if (a[i - k] >= a[i] || a[i + k] >= a[i]) { ok = false; break; }
            if (ok) res.Add(i);
        }
        return res.ToArray();
    }

    private static int[] PivotLows(double[] a, int strength)
    {
        var res = new List<int>();
        for (var i = strength; i < a.Length - strength; i++)
        {
            var ok = true;
            for (var k = 1; k <= strength; k++)
                if (a[i - k] <= a[i] || a[i + k] <= a[i]) { ok = false; break; }
            if (ok) res.Add(i);
        }
        return res.ToArray();
    }
}
