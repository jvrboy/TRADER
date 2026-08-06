using NexusBrain.Core;

namespace NexusBrain.Indicators;

/// <summary>
/// Volatility family: ATR, Bollinger Bands, Keltner, historical/realized volatility,
/// Parkinson/Garman-Klass estimators, and the two Deriv synthetic indices the brain
/// is trained on — the Volatility Index and the Drift Switch Index.
/// </summary>
public static class Volatility
{
    public static double[] Atr(double[] h, double[] l, double[] c, int p = 14)
        => SeriesMath.Atr(h, l, c, p);

    /// <summary>Realized annualised volatility of log returns.</summary>
    public static double[] RealizedVol(double[] c, int p = 20)
    {
        var n = c.Length; var r = new double[n];
        for (int i = 0; i < n; i++)
        {
            int s = Math.Max(1, i - p + 1);
            double sum = 0, sumSq = 0; int cnt = 0;
            for (int j = s; j <= i; j++)
            {
                double lr = Math.Log(c[j] / c[j - 1]);
                sum += lr; sumSq += lr * lr; cnt++;
            }
            if (cnt > 1)
            {
                double mean = sum / cnt;
                double var = (sumSq - cnt * mean * mean) / (cnt - 1);
                r[i] = Math.Sqrt(Math.Max(0, var)) * Math.Sqrt(252);
            }
        }
        return r;
    }

    /// <summary>Parkinson volatility (uses high/low only — robust to gaps).</summary>
    public static double[] ParkinsonVol(double[] h, double[] l, int p = 20)
    {
        var n = h.Length; var r = new double[n];
        for (int i = 0; i < n; i++)
        {
            int s = Math.Max(1, i - p + 1); double sum = 0; int cnt = 0;
            for (int j = s; j <= i; j++)
            {
                if (h[j] <= 0 || l[j] <= 0) continue;
                sum += Math.Pow(Math.Log(h[j] / l[j]), 2) / (4 * Math.Log(2));
                cnt++;
            }
            if (cnt > 0) r[i] = Math.Sqrt(sum / cnt) * Math.Sqrt(252);
        }
        return r;
    }

    public static (double[] mid, double[] upper, double[] lower) Bollinger(double[] c, int p = 20, double k = 2.0)
    {
        var mid = SeriesMath.Sma(c, p);
        var n = c.Length; var up = new double[n]; var lo = new double[n];
        for (int i = 0; i < n; i++)
        {
            if (double.IsNaN(mid[i])) continue;
            double s = 0;
            for (int j = i - p + 1; j <= i; j++) s += (c[j] - mid[i]) * (c[j] - mid[i]);
            double sd = Math.Sqrt(s / p);
            up[i] = mid[i] + k * sd; lo[i] = mid[i] - k * sd;
        }
        return (mid, up, lo);
    }

    public static (double[] mid, double[] upper, double[] lower) Keltner(double[] h, double[] l, double[] c, int p = 20, double k = 2.0)
    {
        var mid = SeriesMath.Ema(c, p);
        var atr = SeriesMath.Atr(h, l, c, p);
        var n = c.Length; var up = new double[n]; var lo = new double[n];
        for (int i = 0; i < n; i++) { up[i] = mid[i] + k * atr[i]; lo[i] = mid[i] - k * atr[i]; }
        return (mid, up, lo);
    }

    /// <summary>
    /// Volatility Index (VI) — the Deriv synthetic index. Measures overall market
    /// volatility; high values mean violent swings, low values mean quiet drift.
    /// Here we model it as a normalised composite of realised vol, ATR ratio and
    /// price-range expansion so the brain can reason about it without live data.
    /// </summary>
    public static double[] VolatilityIndex(double[] c, double[] h, double[] l, int p = 20)
    {
        var rv = RealizedVol(c, p);
        var atr = SeriesMath.Atr(h, l, c, p);
        var n = c.Length; var vi = new double[n];
        for (int i = 0; i < n; i++)
        {
            if (i < p || c[i] <= 0) { vi[i] = 0.5; continue; }
            double atrPct = atr[i] / c[i];
            double rvNorm = SeriesMath.Sigmoid(rv[i] / 0.5);   // squash realised vol
            double atrNorm = SeriesMath.Sigmoid(atrPct * 500); // squash ATR%
            vi[i] = Math.Clamp(0.5 * rvNorm + 0.5 * atrNorm, 0, 1);
        }
        return vi;
    }

    /// <summary>
    /// Drift Switch Index (DSI) — the Deriv synthetic index that alternates between
    /// trending (drifting) and ranging (switching) regimes. High DSI = strong trend,
    /// low DSI = range-bound chop. Modeled as a normalised trend-strength score.
    /// </summary>
    public static double[] DriftSwitchIndex(double[] c, double[] h, double[] l, int p = 20)
    {
        var n = c.Length; var dsi = new double[n];
        var (adx, _, _) = Trend.Adx(h, l, c, 14);
        var macd = Trend.Macd(c);
        for (int i = 0; i < n; i++)
        {
            if (i < p) { dsi[i] = 0.5; continue; }
            // ADX component (trend strength 0..100 -> 0..1)
            double adxNorm = double.IsNaN(adx[i]) ? 0.5 : adx[i] / 100.0;
            // MACD momentum component
            double macdAbs = Math.Abs(macd.hist[i]);
            double macdNorm = SeriesMath.Sigmoid(macdAbs / (c[i] * 0.005 + 1e-12));
            dsi[i] = Math.Clamp(0.6 * adxNorm + 0.4 * macdNorm, 0, 1);
        }
        return dsi;
    }

    /// <summary>Volatility regime label from a VI value.</summary>
    public static string RegimeLabel(double vi)
        => vi < 0.35 ? "LOW_VOLATILITY"
         : vi < 0.6 ? "NORMAL_VOLATILITY"
         : vi < 0.8 ? "HIGH_VOLATILITY"
         : "EXTREME_VOLATILITY";

    /// <summary>Trend regime label from a DSI value.</summary>
    public static string DriftLabel(double dsi)
        => dsi < 0.4 ? "RANGING"
         : dsi < 0.65 ? "MIXED"
         : "TRENDING";
}
