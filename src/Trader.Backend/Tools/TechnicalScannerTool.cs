using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Scans a market series and computes technical signals: RSI, EMA trend,
/// ATR volatility, and a simple composite score. Pure computation, no I/O.
/// </summary>
public sealed class TechnicalScannerTool : ITool
{
    public string Name => "tech.scan";
    public string Description => "Compute RSI, EMA trend and ATR from candle history.";
    public string Parameters => "symbol, period=14";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? context.Market.FirstOrDefault()?.Symbol ?? "?";
        var period = int.TryParse(args.GetValueOrDefault("period"), out var p) && p > 1 ? p : 14;

        var closes = context.Market.Where(c => c.Symbol == symbol).Select(c => c.Close).ToArray();
        if (closes.Length < Math.Max(period + 1, 20))
            return Task.FromResult(ToolResult.Fail($"Not enough data for '{symbol}' to compute indicators."));

        var rsi = Rsi(closes, period);
        var emaFast = Ema(closes, 9);
        var emaSlow = Ema(closes, 21);
        var atr = Atr(context.Market.Where(c => c.Symbol == symbol).ToArray(), 14);

        var lastRsi = rsi;
        var trend = emaFast[^1] > emaSlow[^1] ? "bullish" : "bearish";

        // Simple composite score in [-100, 100]
        var score = (lastRsi - 50) * 1.5;
        if (trend == "bullish") score += 10; else score -= 10;
        score = Math.Clamp(score, -100, 100);

        var data = new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["rsi"] = Math.Round(lastRsi, 2),
            ["trend"] = trend,
            ["ema9"] = Math.Round(emaFast[^1], 4),
            ["ema21"] = Math.Round(emaSlow[^1], 4),
            ["atr14"] = Math.Round(atr, 4),
            ["score"] = Math.Round(score, 1),
            ["lastClose"] = Math.Round(closes[^1], 4),
        };

        var message = $"{symbol}: RSI {data["rsi"]}, {trend} trend, ATR {data["atr14"]}, score {data["score"]}";
        return Task.FromResult(ToolResult.Ok(message, data));
    }

    private static double Rsi(double[] closes, int period)
    {
        if (closes.Length < period + 1) return 50;
        double gain = 0, loss = 0;
        for (var i = closes.Length - period; i < closes.Length - 1; i++)
        {
            var diff = closes[i + 1] - closes[i];
            if (diff >= 0) gain += diff; else loss -= diff;
        }
        if (loss == 0) return 100;
        var rs = gain / period / (loss / period);
        return 100 - (100 / (1 + rs));
    }

    private static double[] Ema(double[] closes, int period)
    {
        var k = 2.0 / (period + 1);
        var ema = new double[closes.Length];
        ema[0] = closes[0];
        for (var i = 1; i < closes.Length; i++)
            ema[i] = closes[i] * k + ema[i - 1] * (1 - k);
        return ema;
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
