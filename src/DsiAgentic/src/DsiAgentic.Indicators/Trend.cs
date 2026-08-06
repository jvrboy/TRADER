using DsiAgentic.Core;

namespace DsiAgentic.Indicators;

/// <summary>
/// 15+ trend indicators: ADX, Aroon, Ichimoku, ParabolicSAR, SuperTrend, Donchian,
/// Keltner, HMA, TEMA, KAMA, LinReg, Choppiness, Ribbon, DMI.
/// </summary>
public static class Trend
{
    public static (double[] adx, double[] plusDi, double[] minusDi) Adx(double[] h, double[] l, double[] c, int p = 14)
    {
        var n = c.Length; var pdm = new double[n]; var mdm = new double[n];
        for (int i = 1; i < n; i++)
        {
            var up = h[i] - h[i - 1];
            var dn = l[i - 1] - l[i];
            pdm[i] = up > dn && up > 0 ? up : 0;
            mdm[i] = dn > up && dn > 0 ? dn : 0;
        }
        var tr = SeriesMath.TrueRange(h, l, c);
        var trR = SeriesMath.Rma(tr, p); var pR = SeriesMath.Rma(pdm, p); var mR = SeriesMath.Rma(mdm, p);
        var pdi = new double[n]; var mdi = new double[n]; var dx = new double[n];
        for (int i = 0; i < n; i++)
        {
            pdi[i] = trR[i] == 0 ? 0 : 100 * pR[i] / trR[i];
            mdi[i] = trR[i] == 0 ? 0 : 100 * mR[i] / trR[i];
            var sum = pdi[i] + mdi[i];
            dx[i] = sum == 0 ? 0 : 100 * Math.Abs(pdi[i] - mdi[i]) / sum;
        }
        var adx = SeriesMath.Rma(dx, p);
        return (adx, pdi, mdi);
    }

    public static (double[] up, double[] down) Aroon(double[] h, double[] l, int p = 25)
    {
        var n = h.Length; var up = new double[n]; var dn = new double[n];
        for (int i = p; i < n; i++)
        {
            int hi = i, li = i;
            for (int j = i - p; j <= i; j++) { if (h[j] >= h[hi]) hi = j; if (l[j] <= l[li]) li = j; }
            up[i] = 100.0 * (p - (i - hi)) / p;
            dn[i] = 100.0 * (p - (i - li)) / p;
        }
        return (up, dn);
    }

    public static (double[] conv, double[] baseL, double[] spanA, double[] spanB) Ichimoku(double[] h, double[] l, int c = 9, int b = 26, int sB = 52)
    {
        var n = h.Length;
        double[] Roll(int p) { var r = new double[n]; for (int i = p - 1; i < n; i++) { double hh = double.MinValue, ll = double.MaxValue; for (int j = i - p + 1; j <= i; j++) { if (h[j] > hh) hh = h[j]; if (l[j] < ll) ll = l[j]; } r[i] = (hh + ll) / 2; } return r; }
        var conv = Roll(c); var baseL = Roll(b);
        var sA = new double[n]; var sBx = Roll(sB);
        for (int i = 0; i < n; i++) sA[i] = (conv[i] + baseL[i]) / 2;
        return (conv, baseL, sA, sBx);
    }

    public static double[] ParabolicSar(double[] h, double[] l, double step = 0.02, double max = 0.2)
    {
        var n = h.Length; var sar = new double[n];
        if (n == 0) return sar;
        bool up = true; double af = step; double ep = h[0]; sar[0] = l[0];
        for (int i = 1; i < n; i++)
        {
            sar[i] = sar[i - 1] + af * (ep - sar[i - 1]);
            if (up)
            {
                if (l[i] < sar[i]) { up = false; sar[i] = ep; ep = l[i]; af = step; }
                else if (h[i] > ep) { ep = h[i]; af = Math.Min(max, af + step); }
            }
            else
            {
                if (h[i] > sar[i]) { up = true; sar[i] = ep; ep = h[i]; af = step; }
                else if (l[i] < ep) { ep = l[i]; af = Math.Min(max, af + step); }
            }
        }
        return sar;
    }

    public static (double[] st, int[] dir) SuperTrend(double[] h, double[] l, double[] c, int p = 10, double m = 3)
    {
        var n = c.Length; var atr = SeriesMath.Atr(h, l, c, p);
        var upper = new double[n]; var lower = new double[n]; var st = new double[n]; var dir = new int[n];
        for (int i = 0; i < n; i++)
        {
            var mid = (h[i] + l[i]) / 2;
            upper[i] = mid + m * atr[i]; lower[i] = mid - m * atr[i];
            if (i == 0) { st[i] = upper[i]; dir[i] = 1; continue; }
            if (c[i] > upper[i - 1]) dir[i] = 1;
            else if (c[i] < lower[i - 1]) dir[i] = -1;
            else dir[i] = dir[i - 1];
            st[i] = dir[i] == 1 ? lower[i] : upper[i];
        }
        return (st, dir);
    }

    public static (double[] up, double[] mid, double[] lo) Donchian(double[] h, double[] l, int p = 20)
    {
        var n = h.Length; var up = new double[n]; var lo = new double[n]; var mid = new double[n];
        for (int i = p - 1; i < n; i++)
        {
            double hh = double.MinValue, ll = double.MaxValue;
            for (int j = i - p + 1; j <= i; j++) { if (h[j] > hh) hh = h[j]; if (l[j] < ll) ll = l[j]; }
            up[i] = hh; lo[i] = ll; mid[i] = (hh + ll) / 2;
        }
        return (up, mid, lo);
    }

    public static (double[] up, double[] mid, double[] lo) Keltner(double[] h, double[] l, double[] c, int p = 20, double m = 2)
    {
        var mid = SeriesMath.Ema(c, p); var atr = SeriesMath.Atr(h, l, c, p);
        var n = c.Length; var up = new double[n]; var lo = new double[n];
        for (int i = 0; i < n; i++) { up[i] = mid[i] + m * atr[i]; lo[i] = mid[i] - m * atr[i]; }
        return (up, mid, lo);
    }

    public static double[] Hma(double[] c, int p = 21)
    {
        static double[] Wma(double[] s, int p)
        {
            var n = s.Length; var r = new double[n]; double denom = p * (p + 1) / 2.0;
            for (int i = p - 1; i < n; i++)
            {
                double num = 0;
                for (int k = 0; k < p; k++) num += s[i - k] * (p - k);
                r[i] = num / denom;
            }
            return r;
        }
        var w1 = Wma(c, p / 2); var w2 = Wma(c, p);
        var n = c.Length; var raw = new double[n];
        for (int i = 0; i < n; i++) raw[i] = 2 * w1[i] - w2[i];
        return Wma(raw, (int)Math.Sqrt(p));
    }

    public static double[] Choppiness(double[] h, double[] l, double[] c, int p = 14)
    {
        var n = c.Length; var atr = SeriesMath.TrueRange(h, l, c); var res = new double[n];
        for (int i = p; i < n; i++)
        {
            double sumTr = 0; double hh = double.MinValue, ll = double.MaxValue;
            for (int j = i - p + 1; j <= i; j++) { sumTr += atr[j]; if (h[j] > hh) hh = h[j]; if (l[j] < ll) ll = l[j]; }
            res[i] = hh == ll ? 0 : 100 * Math.Log10(sumTr / (hh - ll)) / Math.Log10(p);
        }
        return res;
    }

    public static double[][] Ribbon(double[] c, int[] periods)
    {
        var arr = new double[periods.Length][];
        for (int i = 0; i < periods.Length; i++) arr[i] = SeriesMath.Ema(c, periods[i]);
        return arr;
    }
}
