using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Detailed volatility analysis: realized volatility, ATR and ATR% (volatility
/// as a % of price), plus a volatility regime label. Useful for sizing and
/// filter logic.
/// </summary>
public sealed class VolatilityTool : ITool
{
    public string Name => "analysis.volatility";
    public string Description => "Realized volatility, ATR and volatility regime.";
    public string Parameters => "symbol, period=14";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? context.Market.FirstOrDefault()?.Symbol ?? "?";
        var period = int.TryParse(args.GetValueOrDefault("period"), out var p) && p > 1 ? p : 14;

        var candles = context.Market.Where(c => c.Symbol == symbol).OrderBy(c => c.EpochSec).ToArray();
        if (candles.Length < Math.Max(period + 1, 20))
            return Task.FromResult(ToolResult.Fail($"Not enough data for '{symbol}'."));

        var closes = candles.Select(c => c.Close).ToArray();
        var returns = new double[closes.Length - 1];
        for (var i = 0; i < returns.Length; i++)
            returns[i] = (closes[i + 1] / closes[i]) - 1.0;

        var mean = returns.Average();
        var std = StdDev(returns, mean);
        var realizedVol = std * Math.Sqrt(returns.Length); // window annualization proxy

        var atr = Atr(candles, period);
        var atrPct = closes[^1] == 0 ? 0 : (atr / closes[^1]) * 100.0;

        var regime = realizedVol switch
        {
            > 0.15 => "high",
            > 0.07 => "elevated",
            > 0.03 => "normal",
            _ => "low"
        };

        var data = new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["realizedVolPct"] = Math.Round(realizedVol * 100.0, 2),
            ["atr"] = Math.Round(atr, 4),
            ["atrPct"] = Math.Round(atrPct, 2),
            ["regime"] = regime,
            ["lastClose"] = Math.Round(closes[^1], 4),
        };

        var message = $"{symbol}: vol {data["realizedVolPct"]}%, ATR {data["atr"]} ({data["atrPct"]}%), {regime} volatility.";
        return Task.FromResult(ToolResult.Ok(message, data));
    }

    private static double StdDev(double[] values, double mean)
    {
        if (values.Length < 2) return 0;
        return Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / (values.Length - 1));
    }

    private static double Atr(CandleData[] candles, int period)
    {
        if (candles.Length < 2) return 0;
        var trs = new double[candles.Length - 1];
        for (var i = 1; i < candles.Length; i++)
        {
            var h = candles[i].High;
            var l = candles[i].Low;
            var pc = candles[i - 1].Close;
            trs[i - 1] = Math.Max(h - l, Math.Max(Math.Abs(h - pc), Math.Abs(l - pc)));
        }
        var n = Math.Min(period, trs.Length);
        return trs[^n..].Average();
    }
}
