using NexusBrain.Core;
using NexusBrain.Indicators;

namespace NexusBrain.Divergence;

/// <summary>Type of divergence detected.</summary>
public enum DivergenceType
{
    None = 0,
    BullishRegular,     // price lower low, indicator higher low → reversal up
    BearishRegular,     // price higher high, indicator lower high → reversal down
    BullishHidden,      // price higher low, indicator lower low → continuation up
    BearishHidden       // price lower high, indicator higher high → continuation down
}

/// <summary>A detected divergence between price and an oscillator.</summary>
public sealed record Divergence(
    DivergenceType Type,
    string Indicator,
    double Strength,       // 0..1
    int PricePivotIndex,
    int IndicatorPivotIndex,
    string Symbol);

/// <summary>
/// Divergence analysis engine: detects regular and hidden divergences between
/// price swing points and RSI / MACD / Stochastic oscillator swing points.
/// </summary>
public static class DivergenceEngine
{
    /// <summary>
    /// Scan a candle series for divergences across RSI, MACD histogram and StochRSI.
    /// Returns a list of detected divergences.
    /// </summary>
    public static List<Divergence> Scan(Candle[] candles, string symbol = "SYNTH",
        int pivotRadius = 3, double minStrength = 0.5)
    {
        var results = new List<Divergence>();
        if (candles.Length < 40) return results;

        var n = candles.Length;
        var c = new double[n]; var h = new double[n]; var l = new double[n];
        for (int i = 0; i < n; i++) { c[i] = candles[i].Close; h[i] = candles[i].High; l[i] = candles[i].Low; }

        var rsi = Oscillators.Rsi(c, 14);
        var (macd, _, hist) = Trend.Macd(c);
        var stochRsi = Oscillators.StochRsi(c, 14);

        // Detect price swing points
        var (priceHighs, priceLows) = SwingPoints(h, l, pivotRadius);

        results.AddRange(ScanOne(c, rsi, priceHighs, priceLows, "RSI", symbol, minStrength));
        results.AddRange(ScanOne(c, hist, priceHighs, priceLows, "MACD_HIST", symbol, minStrength));
        results.AddRange(ScanOne(c, stochRsi, priceHighs, priceLows, "STOCH_RSI", symbol, minStrength));

        return results;
    }

    private static (List<int> highs, List<int> lows) SwingPoints(double[] h, double[] l, int radius)
    {
        var highs = new List<int>();
        var lows = new List<int>();
        int n = h.Length;
        for (int i = radius; i < n - radius; i++)
        {
            bool isHigh = true, isLow = true;
            for (int j = i - radius; j <= i + radius; j++)
            {
                if (h[j] > h[i]) isHigh = false;
                if (l[j] < l[i]) isLow = false;
            }
            if (isHigh) highs.Add(i);
            if (isLow) lows.Add(i);
        }
        return (highs, lows);
    }

    private static List<Divergence> ScanOne(double[] price, double[] osc,
        List<int> priceHighs, List<int> priceLows,
        string indicator, string symbol, double minStrength)
    {
        var result = new List<Divergence>();
        int n = price.Length;

        // Bearish regular: two price highs rising, two osc highs falling
        for (int i = 0; i < priceHighs.Count - 1; i++)
        {
            int p1 = priceHighs[i], p2 = priceHighs[i + 1];
            if (p2 - p1 < 5) continue;
            if (price[p2] > price[p1] && osc[p2] < osc[p1])
            {
                double strength = DivergenceStrength(price, osc, p1, p2, bearish: true);
                if (strength >= minStrength)
                    result.Add(new Divergence(DivergenceType.BearishRegular, indicator, strength, p2, p1, symbol));
            }
        }

        // Bullish regular: two price lows falling, two osc lows rising
        for (int i = 0; i < priceLows.Count - 1; i++)
        {
            int p1 = priceLows[i], p2 = priceLows[i + 1];
            if (p2 - p1 < 5) continue;
            if (price[p2] < price[p1] && osc[p2] > osc[p1])
            {
                double strength = DivergenceStrength(price, osc, p1, p2, bearish: false);
                if (strength >= minStrength)
                    result.Add(new Divergence(DivergenceType.BullishRegular, indicator, strength, p2, p1, symbol));
            }
        }

        // Bearish hidden: price lower high, osc higher high → continuation down
        for (int i = 0; i < priceHighs.Count - 1; i++)
        {
            int p1 = priceHighs[i], p2 = priceHighs[i + 1];
            if (p2 - p1 < 5) continue;
            if (price[p2] < price[p1] && osc[p2] > osc[p1])
            {
                double strength = DivergenceStrength(price, osc, p1, p2, bearish: true);
                if (strength >= minStrength)
                    result.Add(new Divergence(DivergenceType.BearishHidden, indicator, strength, p2, p1, symbol));
            }
        }

        // Bullish hidden: price higher low, osc lower low → continuation up
        for (int i = 0; i < priceLows.Count - 1; i++)
        {
            int p1 = priceLows[i], p2 = priceLows[i + 1];
            if (p2 - p1 < 5) continue;
            if (price[p2] > price[p1] && osc[p2] < osc[p1])
            {
                double strength = DivergenceStrength(price, osc, p1, p2, bearish: false);
                if (strength >= minStrength)
                    result.Add(new Divergence(DivergenceType.BullishHidden, indicator, strength, p2, p1, symbol));
            }
        }

        return result;
    }

    /// <summary>Normalised divergence strength based on pivot separation and oscillator extremity.</summary>
    private static double DivergenceStrength(double[] price, double[] osc, int p1, int p2, bool bearish)
    {
        double priceSep = Math.Abs(price[p2] - price[p1]) / (price[p1] + 1e-12);
        double oscSep = Math.Abs(osc[p2] - osc[p1]);
        double priceContrib = Math.Clamp(priceSep / 0.02, 0, 1);   // 2% move = full strength
        double oscContrib = Math.Clamp(oscSep / 20.0, 0, 1);        // 20 oscillator pts = full
        // Extremity bonus: divergences at oscillator extremes are stronger
        double oscLevel = osc[p2];
        double extremity = bearish
            ? Math.Clamp((oscLevel - 60) / 40, 0, 1)   // high RSI zone
            : Math.Clamp((40 - oscLevel) / 40, 0, 1);  // low RSI zone
        return Math.Clamp(0.4 * priceContrib + 0.4 * oscContrib + 0.2 * extremity, 0, 1);
    }

    /// <summary>Aggregate the strongest signal from a set of divergences.</summary>
    public static (DivergenceType Type, double Strength) Aggregate(IEnumerable<Divergence> divergences)
    {
        var list = divergences.ToList();
        if (list.Count == 0) return (DivergenceType.None, 0);
        // Prefer regular divergences (stronger reversal signal); weight by strength
        double bullScore = 0, bearScore = 0;
        foreach (var d in list)
        {
            if (d.Type is DivergenceType.BullishRegular or DivergenceType.BullishHidden) bullScore += d.Strength;
            if (d.Type is DivergenceType.BearishRegular or DivergenceType.BearishHidden) bearScore += d.Strength;
        }
        if (bullScore == 0 && bearScore == 0) return (DivergenceType.None, 0);
        if (bullScore > bearScore) return (DivergenceType.BullishRegular, Math.Clamp(bullScore / list.Count, 0, 1));
        return (DivergenceType.BearishRegular, Math.Clamp(bearScore / list.Count, 0, 1));
    }
}
