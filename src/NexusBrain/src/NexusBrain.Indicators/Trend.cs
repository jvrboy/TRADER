using NexusBrain.Core;

namespace NexusBrain.Indicators;

/// <summary>Trend-following indicators: MA family, MACD, ADX, Parabolic SAR, Ichimoku, SuperTrend.</summary>
public static class Trend
{
    public static double[] Wma(double[] v, int p)
    {
        var n = v.Length; var r = new double[n];
        double wsum = p * (p + 1) / 2.0;
        for (int i = p - 1; i < n; i++)
        {
            double s = 0;
            for (int j = 0; j < p; j++) s += v[i - j] * (p - j);
            r[i] = s / wsum;
        }
        return r;
    }

    public static double[] Hma(double[] v, int p)
    {
        int half = p / 2, sq = (int)Math.Sqrt(p);
        var w1 = Wma(v, half);
        var w2 = Wma(v, p);
        var n = v.Length; var diff = new double[n];
        for (int i = 0; i < n; i++) diff[i] = 2 * w1[i] - w2[i];
        return Wma(diff, sq);
    }

    public static (double[] macd, double[] signal, double[] hist) Macd(double[] c, int fast = 12, int slow = 26, int sig = 9)
    {
        var ef = SeriesMath.Ema(c, fast);
        var es = SeriesMath.Ema(c, slow);
        var n = c.Length; var m = new double[n];
        for (int i = 0; i < n; i++) m[i] = ef[i] - es[i];
        var s = SeriesMath.Ema(m, sig);
        var h = new double[n];
        for (int i = 0; i < n; i++) h[i] = m[i] - s[i];
        return (m, s, h);
    }

    public static (double[] adx, double[] plusDi, double[] minusDi) Adx(double[] h, double[] l, double[] c, int p = 14)
    {
        var n = c.Length;
        var up = new double[n]; var dn = new double[n]; var tr = new double[n];
        for (int i = 1; i < n; i++)
        {
            up[i] = h[i] - h[i - 1];
            dn[i] = l[i - 1] - l[i];
            tr[i] = Math.Max(h[i] - l[i], Math.Max(Math.Abs(h[i] - c[i - 1]), Math.Abs(l[i] - c[i - 1])));
            if (up[i] < 0 || up[i] < dn[i]) up[i] = 0;
            if (dn[i] < 0 || dn[i] < up[i]) dn[i] = 0;
        }
        var atr = SeriesMath.Rma(tr, p);
        var sup = SeriesMath.Rma(up, p);
        var sdn = SeriesMath.Rma(dn, p);
        var plusDi = new double[n]; var minusDi = new double[n]; var dx = new double[n];
        for (int i = 0; i < n; i++)
        {
            plusDi[i] = atr[i] == 0 || double.IsNaN(atr[i]) ? 0 : 100 * sup[i] / atr[i];
            minusDi[i] = atr[i] == 0 || double.IsNaN(atr[i]) ? 0 : 100 * sdn[i] / atr[i];
            var s = plusDi[i] + minusDi[i];
            dx[i] = s == 0 ? 0 : 100 * Math.Abs(plusDi[i] - minusDi[i]) / s;
        }
        var adx = SeriesMath.Rma(dx, p);
        return (adx, plusDi, minusDi);
    }

    public static (double[] sar, double[] trend) ParabolicSar(double[] h, double[] l, double step = 0.02, double maxStep = 0.2)
    {
        var n = h.Length;
        var sar = new double[n]; var trend = new double[n];
        if (n < 2) return (sar, trend);
        bool isUp = true;
        double af = step, ep = l[0], prevSar = l[0];
        double prevHigh = h[0], prevLow = l[0];
        for (int i = 1; i < n; i++)
        {
            sar[i] = prevSar + af * (ep - prevSar);
            if (isUp)
            {
                sar[i] = Math.Min(sar[i], prevLow);
                if (h[i] > ep) { ep = h[i]; af = Math.Min(af + step, maxStep); }
                if (l[i] < sar[i]) { isUp = false; sar[i] = ep; ep = l[i]; af = step; }
            }
            else
            {
                sar[i] = Math.Max(sar[i], prevHigh);
                if (l[i] < ep) { ep = l[i]; af = Math.Min(af + step, maxStep); }
                if (h[i] > sar[i]) { isUp = true; sar[i] = ep; ep = h[i]; af = step; }
            }
            prevHigh = h[i]; prevLow = l[i]; prevSar = sar[i];
            trend[i] = isUp ? 1 : -1;
        }
        return (sar, trend);
    }

    public static double[] SuperTrend(double[] h, double[] l, double[] c, int p = 10, double mult = 3.0)
    {
        var n = c.Length;
        var atr = SeriesMath.Atr(h, l, c, p);
        var upper = new double[n]; var lower = new double[n]; var st = new double[n];
        var dir = new double[n];
        for (int i = 0; i < n; i++)
        {
            if (i == 0) { upper[i] = 0; lower[i] = 0; st[i] = c[0]; dir[i] = 1; continue; }
            double hl2 = (h[i] + l[i]) / 2.0;
            upper[i] = hl2 + mult * atr[i];
            lower[i] = hl2 - mult * atr[i];
            if (c[i - 1] <= upper[i - 1]) upper[i] = Math.Min(upper[i], upper[i - 1]);
            if (c[i - 1] >= lower[i - 1]) lower[i] = Math.Max(lower[i], lower[i - 1]);
            if (dir[i - 1] == 1 && c[i] < lower[i]) dir[i] = -1;
            else if (dir[i - 1] == -1 && c[i] > upper[i]) dir[i] = 1;
            else dir[i] = dir[i - 1];
            st[i] = dir[i] == 1 ? lower[i] : upper[i];
        }
        return st;
    }
}
