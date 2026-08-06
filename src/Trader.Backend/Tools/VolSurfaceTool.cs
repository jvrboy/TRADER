using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// A simplified implied-volatility surface model. Given an at-the-money
/// volatility, builds a small surface by applying a skew (vol smile) across
/// strikes and a term structure across expiries. Useful for options-aware
/// analysis and for illustrating how IV varies by strike and tenor.
/// </summary>
public sealed class VolSurfaceTool : ITool
{
    public string Name => "analysis.volsurface";
    public string Description => "Build a simplified implied-volatility surface.";
    public string Parameters => "symbol, atmVol=20, spot=100, skew=0.05";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? context.Market.FirstOrDefault()?.Symbol ?? "?";
        var atmVol = double.TryParse(args.GetValueOrDefault("atmVol"), out var av) && av > 0 ? av : 20.0;
        var spot = double.TryParse(args.GetValueOrDefault("spot"), out var sp) && sp > 0 ? sp : 100.0;
        var skew = double.TryParse(args.GetValueOrDefault("skew"), out var sk) ? sk : 0.05;

        // Moneyness levels: 0.9, 0.95, 1.0, 1.05, 1.1 (put/call)
        var moneyness = new[] { 0.9, 0.95, 1.0, 1.05, 1.1 };
        var expiries = new[] { 7, 30, 90, 180 }; // days

        // Term structure: slight upward slope (vol term premium)
        var rows = new List<Dictionary<string, object>>();
        foreach (var days in expiries)
        {
            var tenorFactor = 1.0 + (days / 365.0) * 0.15;
            var row = new Dictionary<string, object> { ["expiryDays"] = days };
            foreach (var m in moneyness)
            {
                // Skew: lower strike (puts) higher vol, higher strike (calls) lower vol
                var skewFactor = 1.0 - skew * (m - 1.0);
                var iv = atmVol * tenorFactor * skewFactor;
                row[$"m{m:0.00}"] = Math.Round(iv, 2);
            }
            rows.Add(row);
        }

        var data = new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["spot"] = spot,
            ["atmVol"] = atmVol,
            ["skew"] = skew,
            ["surface"] = rows,
        };

        var message = $"{symbol}: IV surface built (ATM {atmVol}%, skew {skew:0.00}, {expiries.Length} tenors).";
        return Task.FromResult(ToolResult.Ok(message, data));
    }
}
