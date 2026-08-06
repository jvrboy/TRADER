using NexusBrain.Core;

namespace NexusBrain.Indicators;

/// <summary>Smart-money concepts: VWAP, OBV, volume profile, order-flow pressure, A/D line.</summary>
public static class SmartMoney
{
    public static double[] Vwap(double[] h, double[] l, double[] c, double[] v, int p)
    {
        var n = c.Length; var r = new double[n];
        double cumPV = 0, cumV = 0;
        for (int i = 0; i < n; i++)
        {
            double tp = (h[i] + l[i] + c[i]) / 3.0;
            double vol = v[i] <= 0 ? 1 : v[i];
            if (i % p == 0 && i > 0) { cumPV = 0; cumV = 0; }
            cumPV += tp * vol; cumV += vol;
            r[i] = cumV == 0 ? 0 : cumPV / cumV;
        }
        return r;
    }

    public static double[] Obv(double[] c, double[] v)
    {
        var n = c.Length; var r = new double[n];
        for (int i = 1; i < n; i++)
            r[i] = r[i - 1] + (c[i] > c[i - 1] ? v[i] : c[i] < c[i - 1] ? -v[i] : 0);
        return r;
    }

    public static double[] AdLine(double[] h, double[] l, double[] c, double[] v)
    {
        var n = c.Length; var r = new double[n];
        for (int i = 0; i < n; i++)
        {
            double mfm = (h[i] - l[i]) == 0 ? 0 : ((c[i] - l[i]) - (h[i] - c[i])) / (h[i] - l[i]);
            double mfv = mfm * (v[i] <= 0 ? 1 : v[i]);
            r[i] = i == 0 ? mfv : r[i - 1] + mfv;
        }
        return r;
    }

    /// <summary>Order-flow pressure: signed volume vs. price move over a window.</summary>
    public static double[] OrderFlowPressure(double[] c, double[] v, int p = 20)
    {
        var n = c.Length; var r = new double[n];
        for (int i = 0; i < n; i++)
        {
            int s = Math.Max(1, i - p + 1);
            double buy = 0, sell = 0;
            for (int j = s; j <= i; j++)
            {
                var vol = v[j] <= 0 ? 1 : v[j];
                if (c[j] >= c[j - 1]) buy += vol; else sell += vol;
            }
            r[i] = (buy + sell) == 0 ? 0 : (buy - sell) / (buy + sell);
        }
        return r;
    }

    /// <summary>Simple volume profile over a window, returning the price level with most volume.</summary>
    public static double VolumeProfilePoc(double[] h, double[] l, double[] v, int p = 100, int bins = 20)
    {
        int n = h.Length;
        int s = Math.Max(0, n - p);
        double mn = double.MaxValue, mx = double.MinValue;
        for (int i = s; i < n; i++) { if (h[i] < mn) mn = h[i]; if (l[i] > mx) mx = l[i]; }
        if (mx <= mn) return 0;
        double binW = (mx - mn) / bins;
        var vol = new double[bins];
        for (int i = s; i < n; i++)
        {
            var vv = v[i] <= 0 ? 1 : v[i];
            int b = (int)((l[i] - mn) / binW);
            if (b >= bins) b = bins - 1;
            vol[b] += vv;
        }
        int maxB = 0;
        for (int i = 1; i < bins; i++) if (vol[i] > vol[maxB]) maxB = i;
        return mn + (maxB + 0.5) * binW;
    }
}
