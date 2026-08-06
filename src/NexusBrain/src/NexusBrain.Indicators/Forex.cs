using NexusBrain.Core;

namespace NexusBrain.Indicators;

/// <summary>Forex-specific analysis: pip math, pivot points, support/resistance, Fibonacci, candlestick patterns.</summary>
public static class Forex
{
    /// <summary>Pip size for a symbol (0.0001 for most pairs, 0.01 for JPY pairs).</summary>
    public static double PipSize(string symbol)
        => symbol.ToUpperInvariant().EndsWith("JPY") || symbol.ToUpperInvariant().Contains("JPY") ? 0.01 : 0.0001;

    public static double ToPips(string symbol, double priceDiff)
        => priceDiff / PipSize(symbol);

    public static double FromPips(string symbol, double pips)
        => pips * PipSize(symbol);

    /// <summary>Classic pivot point levels (P, R1..R3, S1..S3).</summary>
    public static (double P, double R1, double R2, double R3, double S1, double S2, double S3) PivotPoints(double h, double l, double c)
    {
        double p = (h + l + c) / 3.0;
        double r1 = 2 * p - l, s1 = 2 * p - h;
        double r2 = p + (h - l), s2 = p - (h - l);
        double r3 = h + 2 * (p - l), s3 = l - 2 * (h - p);
        return (p, r1, r2, r3, s1, s2, s3);
    }

    /// <summary>Fibonacci retracement levels between a swing low and swing high.</summary>
    public static double[] Fibonacci(double swingLow, double swingHigh)
    {
        double range = swingHigh - swingLow;
        return new[]
        {
            swingHigh - 0.0 * range,
            swingHigh - 0.236 * range,
            swingHigh - 0.382 * range,
            swingHigh - 0.5 * range,
            swingHigh - 0.618 * range,
            swingHigh - 0.786 * range,
            swingLow
        };
    }

    /// <summary>Detect swing highs/lows (fractals) over a window.</summary>
    public static (List<int> highs, List<int> lows) Fractals(double[] h, double[] l, int radius = 2)
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

    /// <summary>Simple support/resistance levels from recent swing points, bucketed.</summary>
    public static List<double> SupportResistance(double[] h, double[] l, int lookback = 200, double bucketPct = 0.002)
    {
        int n = h.Length;
        int s = Math.Max(0, n - lookback);
        var (highs, lows) = Fractals(h, l, 2);
        var levels = new List<double>();
        foreach (var idx in highs) if (idx >= s) levels.Add(h[idx]);
        foreach (var idx in lows) if (idx >= s) levels.Add(l[idx]);
        if (levels.Count == 0) return levels;
        // Bucket nearby levels
        levels.Sort();
        var merged = new List<double>();
        double current = levels[0], count = 1;
        for (int i = 1; i < levels.Count; i++)
        {
            if (Math.Abs(levels[i] - current) / current < bucketPct) { current = (current * count + levels[i]) / (count + 1); count++; }
            else { merged.Add(current); current = levels[i]; count = 1; }
        }
        merged.Add(current);
        return merged;
    }

    /// <summary>Identify common candlestick patterns at the last bar. Returns (name, bias).</summary>
    public static (string Name, Bias Bias) DetectCandlePattern(Candle c, Candle? prev = null, Candle? prev2 = null)
    {
        double body = c.Body, range = c.Range;
        if (range < 1e-12) return ("DOJI", Bias.Neutral);
        double bodyRatio = body / range;

        // Doji
        if (bodyRatio < 0.1) return ("DOJI", Bias.Neutral);

        if (bodyRatio < 0.3)
        {
            // Spinning top / hammer / shooting star
            if (c.LowerWick > 2 * body && c.UpperWick < body) return ("HAMMER", Bias.Bullish);
            if (c.UpperWick > 2 * body && c.LowerWick < body) return ("SHOOTING_STAR", Bias.Bearish);
            return ("SPINNING_TOP", Bias.Neutral);
        }

        // Engulfing
        if (prev is not null)
        {
            if (c.IsBullish && !prev.IsBullish && c.Body > prev.Body && c.Open <= prev.Close && c.Close >= prev.Open)
                return ("BULLISH_ENGULFING", Bias.Bullish);
            if (!c.IsBullish && prev.IsBullish && c.Body > prev.Body && c.Open >= prev.Close && c.Close <= prev.Open)
                return ("BEARISH_ENGULFING", Bias.Bearish);
        }

        // Three methods
        if (prev is not null && prev2 is not null)
        {
            if (c.IsBullish && prev.IsBullish && prev2.IsBullish && c.Close > c.Open)
                return ("THREE_WHITE_SOLDIERS", Bias.Bullish);
            if (!c.IsBullish && !prev.IsBullish && !prev2.IsBullish)
                return ("THREE_BLACK_CROWS", Bias.Bearish);
        }

        // Marubozu
        if (bodyRatio > 0.9) return c.IsBullish ? ("MARUBOZU_BULL", Bias.Bullish) : ("MARUBOZU_BEAR", Bias.Bearish);

        return ("NONE", Bias.Neutral);
    }

    /// <summary>ATR-based position sizing suggestion in units for a given risk %.</summary>
    public static double RiskUnits(double account, double riskPct, double entry, double stop, double pipSize)
    {
        double riskPerUnit = Math.Abs(entry - stop);
        if (riskPerUnit < 1e-12) return 0;
        return (account * riskPct / 100.0) / riskPerUnit;
    }
}
