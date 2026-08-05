using DsiAgentic.Core;

namespace DsiAgentic.Indicators;

public static class Volatility
{
    public static (double[] up, double[] mid, double[] lo) Bollinger(double[] c, int p = 20, double m = 2)
    {
        var mid = SeriesMath.Sma(c, p); var sd = SeriesMath.StdDev(c, p);
        var n = c.Length; var up = new double[n]; var lo = new double[n];
        for (int i = 0; i < n; i++) { up[i] = mid[i] + m * sd[i]; lo[i] = mid[i] - m * sd[i]; }
        return (up, mid, lo);
    }

    public static double[] BbWidth(double[] c, int p = 20, double m = 2)
    {
        var (up, mid, lo) = Bollinger(c, p, m);
        var n = c.Length; var w = new double[n];
        for (int i = 0; i < n; i++) w[i] = mid[i] == 0 ? 0 : (up[i] - lo[i]) / mid[i];
        return w;
    }

    public static double[] VolatilityPercentile(double[] c, int lookback = 100, int window = 14)
    {
        var atrLike = SeriesMath.StdDev(c, window);
        var n = c.Length; var res = new double[n];
        for (int i = lookback; i < n; i++)
        {
            int less = 0, tot = 0;
            for (int j = i - lookback + 1; j <= i; j++) { if (double.IsNaN(atrLike[j])) continue; tot++; if (atrLike[j] < atrLike[i]) less++; }
            res[i] = tot == 0 ? 0 : 100.0 * less / tot;
        }
        return res;
    }
}
