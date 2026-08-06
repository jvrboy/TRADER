namespace DsiAgentic.Indicators;

/// <summary>
/// Statistical tools: Hurst exponent, z-score, Sharpe, fractal dimension, CUSUM.
/// </summary>
public static class Statistical
{
    public static double Hurst(double[] c, int minLag = 2, int maxLag = 20)
    {
        int n = c.Length; if (n < maxLag * 2) return 0.5;
        var lags = new List<int>(); for (int l = minLag; l <= maxLag; l++) lags.Add(l);
        var xs = new List<double>(); var ys = new List<double>();
        foreach (var lag in lags)
        {
            double s = 0; int cnt = 0;
            for (int i = lag; i < n; i++) { var d = c[i] - c[i - lag]; s += d * d; cnt++; }
            if (cnt == 0) continue;
            double rms = Math.Sqrt(s / cnt);
            if (rms <= 0) continue;
            xs.Add(Math.Log(lag)); ys.Add(Math.Log(rms));
        }
        if (xs.Count < 2) return 0.5;
        double mx = xs.Average(), my = ys.Average();
        double num = 0, den = 0;
        for (int i = 0; i < xs.Count; i++) { num += (xs[i] - mx) * (ys[i] - my); den += (xs[i] - mx) * (xs[i] - mx); }
        return den == 0 ? 0.5 : num / den;
    }

    public static double ZScore(double[] c, int lookback = 20)
    {
        int n = c.Length; if (n < lookback) return 0;
        double mean = 0; for (int i = n - lookback; i < n; i++) mean += c[i]; mean /= lookback;
        double v = 0; for (int i = n - lookback; i < n; i++) v += (c[i] - mean) * (c[i] - mean); v /= lookback;
        double sd = Math.Sqrt(v); return sd == 0 ? 0 : (c[^1] - mean) / sd;
    }

    public static double FractalDimension(double[] c, int p = 30)
    {
        int n = c.Length; if (n < p) return 1.5;
        double hh = double.MinValue, ll = double.MaxValue;
        for (int i = n - p; i < n; i++) { if (c[i] > hh) hh = c[i]; if (c[i] < ll) ll = c[i]; }
        double range = hh - ll; if (range == 0) return 1.5;
        double length = 0;
        for (int i = n - p + 1; i < n; i++) length += Math.Abs(c[i] - c[i - 1]);
        return 1 + Math.Log(length / range) / Math.Log(2 * (p - 1));
    }

    public static (double pos, double neg) Cusum(double[] c, double k = 0.5)
    {
        double pos = 0, neg = 0; double mean = 0;
        for (int i = 0; i < c.Length; i++) mean += c[i]; mean /= Math.Max(1, c.Length);
        for (int i = 1; i < c.Length; i++)
        {
            var d = c[i] - c[i - 1];
            pos = Math.Max(0, pos + d - k);
            neg = Math.Min(0, neg + d + k);
        }
        return (pos, neg);
    }
}
