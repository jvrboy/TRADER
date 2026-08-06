using NexusBrain.Core;

namespace NexusBrain.Indicators;

/// <summary>Statistical tools used by the brain: linear regression, R-squared, entropy, autocorrelation, percentile ranks.</summary>
public static class Statistical
{
    /// <summary>Linear regression slope + intercept over a window.</summary>
    public static (double Slope, double Intercept, double RSquared) LinearRegression(double[] v, int p)
    {
        var n = v.Length;
        int s = Math.Max(0, n - p);
        int len = n - s;
        if (len < 2) return (0, 0, 0);
        double sumX = 0, sumY = 0, sumXY = 0, sumXX = 0, sumYY = 0;
        for (int i = 0; i < len; i++)
        {
            double x = i, y = v[s + i];
            sumX += x; sumY += y; sumXY += x * y; sumXX += x * x; sumYY += y * y;
        }
        double denom = len * sumXX - sumX * sumX;
        double slope = denom == 0 ? 0 : (len * sumXY - sumX * sumY) / denom;
        double intercept = (sumY - slope * sumX) / len;
        double rNum = len * sumXY - sumX * sumY;
        double rDen = Math.Sqrt((len * sumXX - sumX * sumX) * (len * sumYY - sumY * sumY));
        double r2 = rDen == 0 ? 0 : (rNum * rNum) / (rDen * rDen);
        return (slope, intercept, r2);
    }

    /// <summary>Shannon entropy of a discretised series (0..1).</summary>
    public static double Entropy(double[] v, int bins = 10)
    {
        if (v.Length == 0) return 0;
        double mn = v.Min(), mx = v.Max();
        if (mx - mn < 1e-12) return 0;
        var counts = new int[bins];
        foreach (var x in v)
        {
            int b = (int)((x - mn) / (mx - mn) * bins);
            if (b >= bins) b = bins - 1;
            counts[b]++;
        }
        double h = 0;
        foreach (var c in counts)
        {
            if (c == 0) continue;
            double p = (double)c / v.Length;
            h -= p * Math.Log(p);
        }
        return h / Math.Log(bins);
    }

    /// <summary>Lag-1 autocorrelation of returns (trend persistence).</summary>
    public static double Autocorrelation(double[] v, int lag = 1)
    {
        var n = v.Length;
        if (n <= lag + 1) return 0;
        var r = new double[n - 1];
        for (int i = 1; i < n; i++) r[i - 1] = v[i] - v[i - 1];
        double m = r.Average();
        double num = 0, den = 0;
        for (int i = 0; i < r.Length - lag; i++)
        {
            num += (r[i] - m) * (r[i + lag] - m);
            den += (r[i] - m) * (r[i] - m);
        }
        return den == 0 ? 0 : num / den;
    }

    /// <summary>Percentile rank of the last value in its window (0..100).</summary>
    public static double PercentileRank(double[] v, int p)
    {
        var n = v.Length;
        int s = Math.Max(0, n - p);
        var window = v[s..n];
        double last = v[n - 1];
        int below = window.Count(x => x < last);
        return 100.0 * below / window.Length;
    }
}
