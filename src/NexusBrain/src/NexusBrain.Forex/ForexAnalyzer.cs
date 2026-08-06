using NexusBrain.Core;
using NexusBrain.Indicators;
using ForexInd = NexusBrain.Indicators.Forex;

namespace NexusBrain.Forex;

/// <summary>Result of a full forex analysis pass.</summary>
public sealed class ForexAnalysis
{
    public required string Symbol { get; init; }
    public double PipSize { get; init; }
    public double LastPrice { get; init; }
    public Bias Bias { get; set; }
    public double Confidence { get; set; }
    public List<double> SupportLevels { get; init; } = new();
    public List<double> ResistanceLevels { get; init; } = new();
    public (double P, double R1, double R2, double R3, double S1, double S2, double S3) Pivots { get; init; }
    public double[] Fibonacci { get; init; } = Array.Empty<double>();
    public string CandlePattern { get; init; } = "NONE";
    public Bias CandleBias { get; init; } = Bias.Neutral;
    public double AtrPips { get; init; }
    public double SuggestedStopPips { get; init; }
    public double SuggestedTargetPips { get; init; }
    public double RiskReward { get; init; }
    public List<string> Notes { get; init; } = new();
}

/// <summary>
/// Forex analysis system: computes pip metrics, pivot points, support/resistance,
/// Fibonacci retracements, candlestick patterns, ATR-based stop/target and
/// risk-reward, then synthesises a directional bias.
/// </summary>
public static class ForexAnalyzer
{
    public static ForexAnalysis Analyze(Candle[] candles, string symbol, double account = 10000, double riskPct = 1.0)
    {
        if (candles.Length < 30)
            throw new ArgumentException("Need at least 30 candles for forex analysis", nameof(candles));

        var n = candles.Length;
        var c = new double[n]; var h = new double[n]; var l = new double[n];
        for (int i = 0; i < n; i++) { c[i] = candles[i].Close; h[i] = candles[i].High; l[i] = candles[i].Low; }

        double pipSize = ForexInd.PipSize(symbol);
        double last = c[n - 1];

        // Pivot points from last full day (last 24 bars as a proxy)
        var lastCandle = candles[n - 1];
        var prevCandle = candles[n - 2];
        var (p, r1, r2, r3, s1, s2, s3) = ForexInd.PivotPoints(prevCandle.High, prevCandle.Low, prevCandle.Close);

        // Support / resistance
        var levels = ForexInd.SupportResistance(h, l, lookback: 200, bucketPct: 0.002);
        var supports = levels.Where(x => x < last).OrderByDescending(x => x).Take(3).ToList();
        var resistances = levels.Where(x => x > last).OrderBy(x => x).Take(3).ToList();

        // Fibonacci from recent swing low/high
        double swingLow = l.Skip(n - 60).Min();
        double swingHigh = h.Skip(n - 60).Max();
        if (swingHigh == swingLow) { swingHigh = last * 1.01; swingLow = last * 0.99; }
        var fib = ForexInd.Fibonacci(swingLow, swingHigh);

        // Candlestick pattern
        var (pattern, patternBias) = ForexInd.DetectCandlePattern(lastCandle, prevCandle, candles.Length > 2 ? candles[n - 3] : null);

        // ATR-based stops/targets
        var atr = SeriesMath.Atr(h, l, c, 14);
        double atrPips = ForexInd.ToPips(symbol, atr[n - 1]);
        double stopPips = Math.Max(atrPips * 1.5, pipSize * 20);
        double targetPips = stopPips * 2.0;
        double riskReward = 2.0;

        // Bias synthesis
        var rsi = Oscillators.Rsi(c, 14);
        var (macd, sig, histArr) = Trend.Macd(c);
        var (adx, pdi, mdi) = Trend.Adx(h, l, c, 14);
        var (k, d) = Oscillators.Stochastic(h, l, c);

        double score = 0;
        var notes = new List<string>();

        // RSI
        if (rsi[n - 1] < 30) { score += 1; notes.Add($"RSI oversold ({rsi[n - 1]:F0})"); }
        else if (rsi[n - 1] > 70) { score -= 1; notes.Add($"RSI overbought ({rsi[n - 1]:F0})"); }

        // MACD
        if (histArr[n - 1] > 0 && histArr[n - 1] > histArr[n - 2]) { score += 1; notes.Add("MACD histogram rising"); }
        else if (histArr[n - 1] < 0 && histArr[n - 1] < histArr[n - 2]) { score -= 1; notes.Add("MACD histogram falling"); }

        // ADX trend
        if (!double.IsNaN(adx[n - 1]))
        {
            if (adx[n - 1] > 25 && pdi[n - 1] > mdi[n - 1]) { score += 1; notes.Add($"Strong uptrend (ADX {adx[n - 1]:F0})"); }
            else if (adx[n - 1] > 25 && mdi[n - 1] > pdi[n - 1]) { score -= 1; notes.Add($"Strong downtrend (ADX {adx[n - 1]:F0})"); }
        }

        // Stochastic
        if (k[n - 1] < 20 && d[n - 1] < 20) { score += 1; notes.Add("Stochastic oversold"); }
        else if (k[n - 1] > 80 && d[n - 1] > 80) { score -= 1; notes.Add("Stochastic overbought"); }

        // Candlestick
        if (patternBias == Bias.Bullish) { score += 1; notes.Add($"Bullish pattern: {pattern}"); }
        else if (patternBias == Bias.Bearish) { score -= 1; notes.Add($"Bearish pattern: {pattern}"); }

        // Position vs pivots
        if (last > r1) { score += 1; notes.Add("Price above R1 (strength)"); }
        else if (last < s1) { score -= 1; notes.Add("Price below S1 (weakness)"); }

        var bias = score > 1 ? Bias.Bullish : score < -1 ? Bias.Bearish : Bias.Neutral;
        double confidence = Math.Clamp(Math.Abs(score) / 5.0, 0, 1);

        return new ForexAnalysis
        {
            Symbol = symbol,
            PipSize = pipSize,
            LastPrice = last,
            Bias = bias,
            Confidence = confidence,
            SupportLevels = supports,
            ResistanceLevels = resistances,
            Pivots = (p, r1, r2, r3, s1, s2, s3),
            Fibonacci = fib,
            CandlePattern = pattern,
            CandleBias = patternBias,
            AtrPips = atrPips,
            SuggestedStopPips = stopPips,
            SuggestedTargetPips = targetPips,
            RiskReward = riskReward,
            Notes = notes
        };
    }

    /// <summary>Position sizing in units for a given entry/stop.</summary>
    public static double PositionSize(double account, double riskPct, double entry, double stop, double pipSize)
        => ForexInd.RiskUnits(account, riskPct, entry, stop, pipSize);
}
