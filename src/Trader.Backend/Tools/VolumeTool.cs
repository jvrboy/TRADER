using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Analyzes volume: relative volume vs the recent average and a volume-based
/// price confirmation signal (up/down on rising volume = stronger move).
/// </summary>
public sealed class VolumeTool : ITool
{
    public string Name => "analysis.volume";
    public string Description => "Relative volume and volume-price confirmation.";
    public string Parameters => "symbol, period=20";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? context.Market.FirstOrDefault()?.Symbol ?? "?";
        var period = int.TryParse(args.GetValueOrDefault("period"), out var p) && p > 1 ? p : 20;

        var candles = context.Market.Where(c => c.Symbol == symbol).OrderBy(c => c.EpochSec).ToArray();
        if (candles.Length < period + 1)
            return Task.FromResult(ToolResult.Fail($"Not enough volume data for '{symbol}'."));

        var volumes = candles.Select(c => c.Volume).ToArray();
        var closes = candles.Select(c => c.Close).ToArray();

        var lastVol = volumes[^1];
        var avgVol = volumes[^period..^1].Average();
        var relVol = avgVol <= 0 ? 0 : lastVol / avgVol;

        // Volume-price confirmation on the last bar
        var priceUp = closes[^1] > closes[^2];
        var volumeStrong = relVol > 1.2;
        var signal = priceUp == volumeStrong
            ? (priceUp ? "confirmed-up" : "confirmed-down")
            : (priceUp ? "weak-up" : "weak-down");

        var data = new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["lastVolume"] = Math.Round(lastVol, 0),
            ["avgVolume"] = Math.Round(avgVol, 0),
            ["relativeVolume"] = Math.Round(relVol, 2),
            ["priceDirection"] = priceUp ? "up" : "down",
            ["signal"] = signal,
        };

        var message = $"{symbol}: rel-vol {relVol:0.00}x, {signal}.";
        return Task.FromResult(ToolResult.Ok(message, data));
    }
}
