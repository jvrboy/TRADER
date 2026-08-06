using NexusBrain.Core;

namespace NexusBrain.Indicators;

/// <summary>Oscillators: RSI, Stochastic, StochRSI, CCI, Williams %R, MFI, CMO, ROC, TSI, Awesome, TRIX, Fisher, Vortex, PPO, MACD-based.</summary>
public static class Oscillators
{
    public static double[] Rsi(double[] c, int p = 14)
    {
        var n = c.Length;
        var g = new double[n]; var l = new double[n];
        for (int i = 1; i < n; i++) { var d = c[i] - c[i - 1]; g[i] = Math.Max(0, d); l[i] = Math.Max(0, -d); }
        var ag = SeriesMath.Rma(g, p); var al = SeriesMath.Rma(l, p);
        var r = new double[n];
        for (int i = 0; i < n; i++)
        {
            if (double.IsNaN(ag[i]) || al[i] == 0) { r[i] = 50; continue; }
            var rs = ag[i] / al[i];
            r[i] = 100 - 100 / (1 + rs);
        }
        return r;
    }

    public static (double[] k, double[] d) Stochastic(double[] h, double[] l, double[] c, int k = 14, int d = 3)
    {
        var n = c.Length; var kk = new double[n];
        for (int i = 0; i < n; i++)
        {
            int s = Math.Max(0, i - k + 1);
            double hh = double.MinValue, ll = double.MaxValue;
            for (int j = s; j <= i; j++) { if (h[j] > hh) hh = h[j]; if (l[j] < ll) ll = l[j]; }
            kk[i] = hh == ll ? 50 : 100 * (c[i] - ll) / (hh - ll);
        }
        return (kk, SeriesMath.Sma(kk, d));
    }

    public static double[] StochRsi(double[] c, int p = 14)
    {
        var rsi = Rsi(c, p);
        var n = rsi.Length; var res = new double[n];
        for (int i = 0; i < n; i++)
        {
            int s = Math.Max(0, i - p + 1);
            double hh = double.MinValue, ll = double.MaxValue;
            for (int j = s; j <= i; j++) { if (rsi[j] > hh) hh = rsi[j]; if (rsi[j] < ll) ll = rsi[j]; }
            res[i] = hh == ll ? 50 : 100 * (rsi[i] - ll) / (hh - ll);
        }
        return res;
    }

    public static double[] Cci(double[] h, double[] l, double[] c, int p = 20)
    {
        var n = c.Length; var tp = new double[n];
        for (int i = 0; i < n; i++) tp[i] = (h[i] + l[i] + c[i]) / 3.0;
        var sma = SeriesMath.Sma(tp, p);
        var res = new double[n];
        for (int i = 0; i < n; i++)
        {
            if (double.IsNaN(sma[i])) { res[i] = 0; continue; }
            double md = 0;
            for (int j = i - p + 1; j <= i; j++) md += Math.Abs(tp[j] - sma[i]);
            md /= p;
            res[i] = md == 0 ? 0 : (tp[i] - sma[i]) / (0.015 * md);
        }
        return res;
    }

    public static double[] WilliamsR(double[] h, double[] l, double[] c, int p = 14)
    {
        var n = c.Length; var res = new double[n];
        for (int i = 0; i < n; i++)
        {
            int s = Math.Max(0, i - p + 1);
            double hh = double.MinValue, ll = double.MaxValue;
            for (int j = s; j <= i; j++) { if (h[j] > hh) hh = h[j]; if (l[j] < ll) ll = l[j]; }
            res[i] = hh == ll ? -50 : -100 * (hh - c[i]) / (hh - ll);
        }
        return res;
    }

    public static double[] Mfi(double[] h, double[] l, double[] c, double[] v, int p = 14)
    {
        var n = c.Length; var pos = new double[n]; var neg = new double[n];
        for (int i = 1; i < n; i++)
        {
            double tp = (h[i] + l[i] + c[i]) / 3.0;
            double ptp = (h[i - 1] + l[i - 1] + c[i - 1]) / 3.0;
            double mf = tp * (v[i] <= 0 ? 1 : v[i]);
            if (tp > ptp) pos[i] = mf; else neg[i] = mf;
        }
        var res = new double[n];
        for (int i = p; i < n; i++)
        {
            double sp = 0, sn = 0;
            for (int j = i - p + 1; j <= i; j++) { sp += pos[j]; sn += neg[j]; }
            if (sn == 0) { res[i] = 100; continue; }
            var mr = sp / sn; res[i] = 100 - 100 / (1 + mr);
        }
        return res;
    }

    public static double[] Cmo(double[] c, int p = 14)
    {
        var n = c.Length; var res = new double[n];
        for (int i = p; i < n; i++)
        {
            double su = 0, sd = 0;
            for (int j = i - p + 1; j <= i; j++)
            {
                var d = c[j] - c[j - 1];
                if (d > 0) su += d; else sd += -d;
            }
            res[i] = (su + sd) == 0 ? 0 : 100 * (su - sd) / (su + sd);
        }
        return res;
    }

    public static double[] Roc(double[] c, int p = 12)
    {
        var n = c.Length; var res = new double[n];
        for (int i = p; i < n; i++) res[i] = c[i - p] == 0 ? 0 : 100 * (c[i] - c[i - p]) / c[i - p];
        return res;
    }

    public static double[] Tsi(double[] c, int r = 25, int s = 13)
    {
        var n = c.Length; var m = new double[n]; var am = new double[n];
        for (int i = 1; i < n; i++) { m[i] = c[i] - c[i - 1]; am[i] = Math.Abs(m[i]); }
        var e1 = SeriesMath.Ema(m, r); var e2 = SeriesMath.Ema(e1, s);
        var a1 = SeriesMath.Ema(am, r); var a2 = SeriesMath.Ema(a1, s);
        var res = new double[n];
        for (int i = 0; i < n; i++) res[i] = a2[i] == 0 || double.IsNaN(a2[i]) ? 0 : 100 * e2[i] / a2[i];
        return res;
    }

    public static double[] Awesome(double[] h, double[] l)
    {
        var n = h.Length; var mp = new double[n];
        for (int i = 0; i < n; i++) mp[i] = (h[i] + l[i]) / 2.0;
        var s5 = SeriesMath.Sma(mp, 5); var s34 = SeriesMath.Sma(mp, 34);
        var r = new double[n];
        for (int i = 0; i < n; i++) r[i] = s5[i] - s34[i];
        return r;
    }

    public static double[] Trix(double[] c, int p = 15)
    {
        var e1 = SeriesMath.Ema(c, p); var e2 = SeriesMath.Ema(e1, p); var e3 = SeriesMath.Ema(e2, p);
        var n = c.Length; var res = new double[n];
        for (int i = 1; i < n; i++) res[i] = e3[i - 1] == 0 ? 0 : 10000 * (e3[i] - e3[i - 1]) / e3[i - 1];
        return res;
    }

    public static double[] Fisher(double[] h, double[] l, int p = 10)
    {
        var n = h.Length; var res = new double[n]; double v1 = 0, f = 0;
        for (int i = 0; i < n; i++)
        {
            int s = Math.Max(0, i - p + 1);
            double hh = double.MinValue, ll = double.MaxValue;
            for (int j = s; j <= i; j++) { var mp = (h[j] + l[j]) / 2.0; if (mp > hh) hh = mp; if (mp < ll) ll = mp; }
            double x = hh == ll ? 0 : 2 * (((h[i] + l[i]) / 2.0 - ll) / (hh - ll) - 0.5);
            v1 = 0.33 * x + 0.67 * v1;
            v1 = Math.Max(-0.999, Math.Min(0.999, v1));
            f = 0.5 * Math.Log((1 + v1) / (1 - v1)) + 0.5 * f;
            res[i] = f;
        }
        return res;
    }

    public static double[] Ppo(double[] c, int fast = 12, int slow = 26)
    {
        var ef = SeriesMath.Ema(c, fast); var es = SeriesMath.Ema(c, slow);
        var n = c.Length; var r = new double[n];
        for (int i = 0; i < n; i++) r[i] = es[i] == 0 ? 0 : 100 * (ef[i] - es[i]) / es[i];
        return r;
    }

    public static double[] Vortex(double[] h, double[] l, double[] c, int p = 14)
    {
        var n = c.Length; var vip = new double[n]; var vim = new double[n];
        var tr = SeriesMath.TrueRange(h, l, c);
        var res = new double[n];
        for (int i = p; i < n; i++)
        {
            double sVp = 0, sVm = 0, sTr = 0;
            for (int j = i - p + 1; j <= i; j++)
            {
                sVp += Math.Abs(h[j] - l[j - 1]);
                sVm += Math.Abs(l[j] - h[j - 1]);
                sTr += tr[j];
            }
            res[i] = sTr == 0 ? 0 : (sVp - sVm) / sTr;
        }
        return res;
    }
}
