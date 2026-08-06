namespace DsiAgentic.Core;

/// <summary>
/// Compact column-oriented candle series used by every indicator and agent.
/// Immutable snapshot semantics; new arrays are appended on refresh, not mutated.
/// </summary>
public sealed class Series
{
    public string Symbol { get; }
    public int TimeframeSec { get; }
    public long[] Epoch { get; private set; }
    public double[] Open { get; private set; }
    public double[] High { get; private set; }
    public double[] Low { get; private set; }
    public double[] Close { get; private set; }
    public double[] Volume { get; private set; }
    public int Count => Close.Length;

    public Series(string symbol, int tf, IReadOnlyList<Candle> candles)
    {
        Symbol = symbol;
        TimeframeSec = tf;
        var n = candles.Count;
        Epoch = new long[n]; Open = new double[n]; High = new double[n];
        Low = new double[n]; Close = new double[n]; Volume = new double[n];
        for (int i = 0; i < n; i++)
        {
            Epoch[i] = candles[i].EpochSec;
            Open[i] = candles[i].Open;
            High[i] = candles[i].High;
            Low[i] = candles[i].Low;
            Close[i] = candles[i].Close;
            Volume[i] = candles[i].Volume;
        }
    }

    public double LastClose => Close[^1];
    public double LastHigh => High[^1];
    public double LastLow => Low[^1];
    public IEnumerable<double> Range(double[] a, int from, int to)
    {
        for (int i = from; i < to; i++) yield return a[i];
    }
}

public static class SeriesMath
{
    public static double[] Sma(double[] src, int period)
    {
        var n = src.Length; var res = new double[n]; double sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += src[i];
            if (i >= period) sum -= src[i - period];
            res[i] = i >= period - 1 ? sum / period : double.NaN;
        }
        return res;
    }

    public static double[] Ema(double[] src, int period)
    {
        var n = src.Length; var res = new double[n]; double k = 2.0 / (period + 1);
        double ema = 0; bool seeded = false;
        for (int i = 0; i < n; i++)
        {
            if (!seeded) { ema = src[i]; seeded = true; }
            else ema = src[i] * k + ema * (1 - k);
            res[i] = i < period - 1 ? double.NaN : ema;
        }
        return res;
    }

    public static double[] Rma(double[] src, int period)
    {
        var n = src.Length; var res = new double[n]; double a = 1.0 / period; double rma = 0; bool seeded = false;
        for (int i = 0; i < n; i++)
        {
            if (!seeded) { rma = src[i]; seeded = true; }
            else rma = a * src[i] + (1 - a) * rma;
            res[i] = i < period - 1 ? double.NaN : rma;
        }
        return res;
    }

    public static double[] StdDev(double[] src, int period)
    {
        var n = src.Length; var res = new double[n];
        for (int i = 0; i < n; i++)
        {
            if (i < period - 1) { res[i] = double.NaN; continue; }
            double mean = 0;
            for (int j = i - period + 1; j <= i; j++) mean += src[j];
            mean /= period;
            double v = 0;
            for (int j = i - period + 1; j <= i; j++) v += (src[j] - mean) * (src[j] - mean);
            res[i] = Math.Sqrt(v / period);
        }
        return res;
    }

    public static double[] TrueRange(double[] h, double[] l, double[] c)
    {
        var n = c.Length; var tr = new double[n];
        tr[0] = h[0] - l[0];
        for (int i = 1; i < n; i++)
        {
            var a = h[i] - l[i];
            var b = Math.Abs(h[i] - c[i - 1]);
            var d = Math.Abs(l[i] - c[i - 1]);
            tr[i] = Math.Max(a, Math.Max(b, d));
        }
        return tr;
    }

    public static double[] Atr(double[] h, double[] l, double[] c, int period = 14)
        => Rma(TrueRange(h, l, c), period);

    public static double LinRegSlope(double[] src, int period)
    {
        int n = Math.Min(period, src.Length);
        if (n < 2) return 0;
        double sx = 0, sy = 0, sxy = 0, sxx = 0;
        int start = src.Length - n;
        for (int i = 0; i < n; i++)
        {
            double x = i, y = src[start + i];
            sx += x; sy += y; sxy += x * y; sxx += x * x;
        }
        double denom = n * sxx - sx * sx;
        return denom == 0 ? 0 : (n * sxy - sx * sy) / denom;
    }

    public static double SafeLast(double[] a) => a.Length == 0 ? double.NaN : a[^1];
}
