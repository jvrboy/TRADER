namespace NexusBrain.Core;

/// <summary>Kind of market instrument the brain can analyse.</summary>
public enum InstrumentKind
{
    Forex,
    SyntheticVolatility,
    SyntheticDriftSwitch,
    Stock,
    Crypto,
    Index
}

/// <summary>Direction bias produced by analysis.</summary>
public enum Bias
{
    Unknown = 0,
    Bullish = 1,
    Bearish = -1,
    Neutral = 0
}

/// <summary>One OHLCV candle with a UTC epoch.</summary>
public sealed record Candle(long Epoch, double Open, double High, double Low, double Close, double Volume)
{
    /// <summary>True when the candle is bullish (close &gt; open).</summary>
    public bool IsBullish => Close >= Open;
    public double Range => High - Low;
    public double Body => Math.Abs(Close - Open);
    public double UpperWick => High - Math.Max(Open, Close);
    public double LowerWick => Math.Min(Open, Close) - Low;
}

/// <summary>Single price tick (used for streaming quotes).</summary>
public sealed record Tick(long Epoch, double Quote, double? Ask = null, double? Bid = null);

/// <summary>A point in a price/indicator series used for divergence detection.</summary>
public sealed record SeriesPoint(int Index, double Value, long Epoch);

/// <summary>Normalised feature vector fed into the neural brain.</summary>
public sealed record FeatureVector(string Name, double[] Values, long Epoch)
{
    public int Dim => Values.Length;
    public override string ToString() => $"{Name}[{Dim}]@{Epoch}";
}

/// <summary>Standard statistics helpers used across the whole brain.</summary>
public static class SeriesMath
{
    public static double[] Sma(double[] v, int p)
    {
        var n = v.Length; var r = new double[n];
        double sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += v[i];
            if (i >= p) sum -= v[i - p];
            if (i >= p - 1) r[i] = sum / p; else r[i] = double.NaN;
        }
        return r;
    }

    public static double[] Ema(double[] v, int p)
    {
        var n = v.Length; var r = new double[n];
        if (n == 0) return r;
        double k = 2.0 / (p + 1.0);
        r[0] = v[0];
        for (int i = 1; i < n; i++) r[i] = v[i] * k + r[i - 1] * (1 - k);
        return r;
    }

    public static double[] Rma(double[] v, int p)
    {
        var n = v.Length; var r = new double[n];
        if (n == 0) return r;
        double sum = 0;
        for (int i = 0; i < n; i++)
        {
            if (i < p) { sum += v[i]; r[i] = double.NaN; }
            else if (i == p) { sum += v[i]; r[i] = sum / p; }
            else r[i] = (r[i - 1] * (p - 1) + v[i]) / p;
        }
        return r;
    }

    public static double[] TrueRange(double[] h, double[] l, double[] c)
    {
        var n = h.Length; var r = new double[n];
        for (int i = 0; i < n; i++)
        {
            if (i == 0) { r[i] = h[i] - l[i]; continue; }
            double pc = c[i - 1];
            r[i] = Math.Max(h[i] - l[i], Math.Max(Math.Abs(h[i] - pc), Math.Abs(l[i] - pc)));
        }
        return r;
    }

    public static double[] Atr(double[] h, double[] l, double[] c, int p = 14)
        => Rma(TrueRange(h, l, c), p);

    /// <summary>Z-score normalisation of a series.</summary>
    public static double[] ZScore(double[] v)
    {
        var n = v.Length; var r = new double[n];
        if (n == 0) return r;
        double mean = v.Average();
        double sd = Math.Sqrt(v.Sum(x => (x - mean) * (x - mean)) / n);
        if (sd < 1e-12) return r;
        for (int i = 0; i < n; i++) r[i] = (v[i] - mean) / sd;
        return r;
    }

    /// <summary>Min-max normalise to [0,1].</summary>
    public static double[] Normalize(double[] v)
    {
        var n = v.Length; var r = new double[n];
        if (n == 0) return r;
        double mn = v.Min(), mx = v.Max();
        if (mx - mn < 1e-12) return r;
        for (int i = 0; i < n; i++) r[i] = (v[i] - mn) / (mx - mn);
        return r;
    }

    public static double Mean(double[] v) => v.Length == 0 ? 0 : v.Average();
    public static double Std(double[] v)
    {
        if (v.Length == 0) return 0;
        double m = v.Average();
        return Math.Sqrt(v.Sum(x => (x - m) * (x - m)) / v.Length);
    }

    /// <summary>Linear correlation coefficient between two series.</summary>
    public static double Correlation(double[] a, double[] b)
    {
        int n = Math.Min(a.Length, b.Length);
        if (n < 2) return 0;
        double ma = 0, mb = 0;
        for (int i = 0; i < n; i++) { ma += a[i]; mb += b[i]; }
        ma /= n; mb /= n;
        double num = 0, da = 0, db = 0;
        for (int i = 0; i < n; i++)
        {
            num += (a[i] - ma) * (b[i] - mb);
            da += (a[i] - ma) * (a[i] - ma);
            db += (b[i] - mb) * (b[i] - mb);
        }
        if (da < 1e-12 || db < 1e-12) return 0;
        return num / Math.Sqrt(da * db);
    }

    /// <summary>Percent change of a series over a lookback.</summary>
    public static double[] PctChange(double[] v, int p = 1)
    {
        var n = v.Length; var r = new double[n];
        for (int i = p; i < n; i++) r[i] = v[i - p] == 0 ? 0 : (v[i] - v[i - p]) / v[i - p];
        return r;
    }

    /// <summary>Simple logistic sigmoid.</summary>
    public static double Sigmoid(double x) => 1.0 / (1.0 + Math.Exp(-x));
}
