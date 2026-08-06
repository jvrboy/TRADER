using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Evaluates trend direction and momentum across multiple timeframes (Short, Medium, Long term)
/// to determine market trend alignment and confluence score.
/// </summary>
public sealed class MultiTimeframeTrendTool : ITool
{
    public string Name => "analysis.mtf";
    public string Description => "Analyzes multi-timeframe trend alignment, momentum confluence, and direction consensus.";

    public IReadOnlyList<ToolParam> Parameters => new[]
    {
        new ToolParam("symbol", "Ticker symbol to analyze", Required: true),
    };

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        if (!args.TryGetValue("symbol", out var symbol) || string.IsNullOrWhiteSpace(symbol))
            return Task.FromResult(ToolResult.Fail("Missing required 'symbol' parameter."));

        var series = context.GetSeries(symbol);
        if (series.Length < 30)
            return Task.FromResult(ToolResult.Fail($"Insufficient data for MTF scan on {symbol}. Need at least 30 candles, got {series.Length}."));

        var closes = series.Select(c => c.Close).ToArray();

        // 1. Short Term (Fast EMA 9 vs EMA 21)
        var ema9 = Ema(closes, 9);
        var ema21 = Ema(closes, 21);
        var shortTermBullish = ema9[^1] > ema21[^1];
        var shortTermScore = (ema9[^1] - ema21[^1]) / ema21[^1] * 1000.0;

        // 2. Medium Term (EMA 50 vs EMA 100)
        var p50 = Math.Min(50, closes.Length - 1);
        var p100 = Math.Min(100, closes.Length - 1);
        var ema50 = Ema(closes, p50);
        var ema100 = Ema(closes, p100);
        var mediumTermBullish = ema50[^1] > ema100[^1];

        // 3. Long Term (Price vs EMA 200 or longest available)
        var p200 = Math.Min(200, closes.Length - 1);
        var ema200 = Ema(closes, p200);
        var longTermBullish = closes[^1] > ema200[^1];

        // 4. Momentum Confluence (RSI 14)
        var rsi = ComputeRsi(closes, 14);
        var rsiBullish = rsi > 50.0;

        var bullishVotes = (shortTermBullish ? 1 : 0) + (mediumTermBullish ? 1 : 0) + (longTermBullish ? 1 : 0) + (rsiBullish ? 1 : 0);
        var confluencePct = Math.Round(bullishVotes / 4.0 * 100.0, 1);

        string consensus;
        if (bullishVotes == 4) consensus = "Strong Bullish Confluence";
        else if (bullishVotes == 3) consensus = "Moderate Bullish";
        else if (bullishVotes == 1) consensus = "Moderate Bearish";
        else if (bullishVotes == 0) consensus = "Strong Bearish Confluence";
        else consensus = "Mixed / Consolidation";

        return Task.FromResult(ToolResult.Ok(
            $"{symbol} MTF Alignment: {consensus} (Bullish Confluence {confluencePct}% | Short: {(shortTermBullish ? "BULL" : "BEAR")}, Med: {(mediumTermBullish ? "BULL" : "BEAR")}, Long: {(longTermBullish ? "BULL" : "BEAR")}, RSI {rsi:0.0}).",
            new Dictionary<string, object>
            {
                ["symbol"] = symbol,
                ["consensus"] = consensus,
                ["confluencePct"] = confluencePct,
                ["shortTerm"] = shortTermBullish ? "bullish" : "bearish",
                ["mediumTerm"] = mediumTermBullish ? "bullish" : "bearish",
                ["longTerm"] = longTermBullish ? "bullish" : "bearish",
                ["rsi"] = Math.Round(rsi, 2),
                ["ema9"] = Math.Round(ema9[^1], 5),
                ["ema21"] = Math.Round(ema21[^1], 5),
                ["ema50"] = Math.Round(ema50[^1], 5),
                ["ema200"] = Math.Round(ema200[^1], 5)
            }));
    }

    private static double[] Ema(double[] values, int period)
    {
        var k = 2.0 / (period + 1);
        var ema = new double[values.Length];
        ema[0] = values[0];
        for (var i = 1; i < values.Length; i++)
            ema[i] = values[i] * k + ema[i - 1] * (1.0 - k);
        return ema;
    }

    private static double ComputeRsi(double[] closes, int period)
    {
        if (closes.Length < period + 1) return 50.0;
        double gain = 0, loss = 0;
        for (var i = closes.Length - period; i < closes.Length - 1; i++)
        {
            var diff = closes[i + 1] - closes[i];
            if (diff >= 0) gain += diff; else loss -= diff;
        }
        if (loss <= 0) return 100.0;
        return 100.0 - (100.0 / (1.0 + (gain / period) / (loss / period)));
    }
}
