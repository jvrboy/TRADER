using DsiAgentic.Core;
using DsiAgentic.Indicators;

namespace DsiAgentic.Brains;

/// <summary>
/// Deterministic 16-dimensional feature vector fed to every brain.
/// Every dimension is normalized to a bounded range so kernels remain stable.
/// </summary>
public static class FeatureExtractor
{
    public const int Dim = 16;

    public static double[] Extract(Series s)
    {
        var x = new double[Dim];
        if (s.Count < 30) return x;
        var c = s.Close; var h = s.High; var l = s.Low;
        var ema20 = SeriesMath.Ema(c, 20);
        var ema50 = SeriesMath.Ema(c, 50);
        var atr = SeriesMath.Atr(h, l, c, 14);
        var rsi = Oscillators.Rsi(c, 14);
        var (kk, dd) = Oscillators.Stochastic(h, l, c);
        var (macd, sig, hist) = Oscillators.Macd(c);
        var (adx, pdi, mdi) = Trend.Adx(h, l, c);
        var bbw = Volatility.BbWidth(c);
        var hurst = Statistical.Hurst(c);
        var zscore = Statistical.ZScore(c);
        var fdim = Statistical.FractalDimension(c);
        var slope = SeriesMath.LinRegSlope(c, 20);

        double N(double v, double lo, double hi) => Math.Max(0, Math.Min(1, (v - lo) / (hi - lo + 1e-9)));

        x[0]  = N(rsi[^1], 0, 100);
        x[1]  = N(kk[^1], 0, 100);
        x[2]  = N(dd[^1], 0, 100);
        x[3]  = N(adx[^1], 0, 60);
        x[4]  = N(pdi[^1] - mdi[^1], -40, 40);
        x[5]  = N(hist[^1], -atr[^1], atr[^1]);
        x[6]  = N((c[^1] - ema20[^1]) / (atr[^1] + 1e-9), -3, 3);
        x[7]  = N((c[^1] - ema50[^1]) / (atr[^1] + 1e-9), -3, 3);
        x[8]  = N(bbw[^1], 0, 0.08);
        x[9]  = N(hurst, 0.3, 0.8);
        x[10] = N(zscore, -3, 3);
        x[11] = N(fdim, 1.0, 2.0);
        x[12] = N(slope / (atr[^1] + 1e-9), -1, 1);
        x[13] = N(atr[^1] / c[^1], 0, 0.02);
        // rolling return
        double ret = (c[^1] / c[Math.Max(0, s.Count - 20)]) - 1.0;
        x[14] = N(ret, -0.05, 0.05);
        // rolling range compression
        double hh = double.MinValue, ll = double.MaxValue;
        int start = Math.Max(0, s.Count - 20);
        for (int i = start; i < s.Count; i++) { if (h[i] > hh) hh = h[i]; if (l[i] < ll) ll = l[i]; }
        x[15] = N((hh - ll) / (c[^1] + 1e-9), 0, 0.05);

        return x;
    }
}
