using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Order-flow analysis. Computes an order-flow imbalance from candle
/// up/down volume and derives a pressure score in [-100, 100] plus a
/// buying/selling pressure label. A proxy for institutional order flow when
/// tick-level data is unavailable.
/// </summary>
public sealed class OrderFlowTool : ITool
{
    public string Name => "analysis.orderflow";
    public string Description => "Order-flow imbalance and buying/selling pressure.";
    public string Parameters => "symbol, lookback=20";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? context.Market.FirstOrDefault()?.Symbol ?? "?";
        var lookback = int.TryParse(args.GetValueOrDefault("lookback"), out var lb) && lb > 2 ? lb : 20;

        var candles = context.Market.Where(c => c.Symbol == symbol).OrderBy(c => c.EpochSec).ToArray();
        if (candles.Length < lookback + 1)
            return Task.FromResult(ToolResult.Fail($"Not enough data for '{symbol}'."));

        double buyVol = 0, sellVol = 0;
        for (var i = candles.Length - lookback; i < candles.Length; i++)
        {
            var c = candles[i];
            if (c.Close >= c.Open) buyVol += c.Volume;
            else sellVol += c.Volume;
        }

        var total = buyVol + sellVol;
        var imbalance = total == 0 ? 0 : (buyVol - sellVol) / total; // -1..1
        var pressure = Math.Clamp(imbalance * 100.0, -100, 100);

        var label = pressure switch
        {
            > 30 => "strong-buying",
            > 5 => "buying",
            < -30 => "strong-selling",
            < -5 => "selling",
            _ => "balanced"
        };

        var data = new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["buyVolume"] = Math.Round(buyVol, 0),
            ["sellVolume"] = Math.Round(sellVol, 0),
            ["imbalance"] = Math.Round(imbalance, 3),
            ["pressure"] = Math.Round(pressure, 1),
            ["label"] = label,
        };

        var message = $"{symbol}: order flow {label} (pressure {pressure:+0;-0}).";
        return Task.FromResult(ToolResult.Ok(message, data));
    }
}
