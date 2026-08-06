using NexusBrain.Core;

namespace NexusBrain.Indicators;

/// <summary>
/// Converts raw candles into a compact, normalised feature vector the neural brain
/// can learn from. Combines trend, momentum, volatility (VI/DSI) and volume features.
/// </summary>
public static class FeatureExtractor
{
    /// <summary>Number of features produced by <see cref="Extract"/>.</summary>
    public const int FeatureCount = 24;

    /// <summary>
    /// Extract a normalised feature vector from a candle series.
    /// Features (0..23):
    ///  0  RSI(14)/100
    ///  1  StochRSI(14)/100
    ///  2  MACD histogram normalised
    ///  3  MACD signal cross (-1..1)
    ///  4  ADX/100 (trend strength)
    ///  5  +DI - -DI (direction)
    ///  6  Volatility Index (VI)
    ///  7  Drift Switch Index (DSI)
    ///  8  ATR% (ATR/close)
    ///  9  Bollinger %B
    /// 10  ROC(10) normalised
    /// 11  CCI(20)/100
    /// 12  Williams %R/100
    /// 13  OBV slope normalised
    /// 14  Order-flow pressure
    /// 15  Price percentile rank (0..1)
    /// 16  lag-1 autocorrelation (trend persistence)
    /// 17  entropy (market structure)
    /// 18  linear regression slope normalised
    /// 19  close vs VWAP
    /// 20  candle body ratio
    /// 21  upper wick ratio
    /// 22  lower wick ratio
    /// 23  short-term momentum (5-bar return normalised)
    /// </summary>
    public static double[] Extract(Candle[] candles, string symbol = "SYNTH")
    {
        if (candles.Length < 40)
            throw new ArgumentException("Need at least 40 candles to extract features", nameof(candles));

        var n = candles.Length;
        var c = new double[n];
        var h = new double[n];
        var l = new double[n];
        var v = new double[n];
        for (int i = 0; i < n; i++)
        {
            c[i] = candles[i].Close;
            h[i] = candles[i].High;
            l[i] = candles[i].Low;
            v[i] = candles[i].Volume;
        }

        var f = new double[FeatureCount];
        double last = c[n - 1], prev = c[n - 2];

        // 0 RSI
        var rsi = Oscillators.Rsi(c, 14);
        f[0] = Math.Clamp(rsi[n - 1] / 100.0, 0, 1);

        // 1 StochRSI
        var srsi = Oscillators.StochRsi(c, 14);
        f[1] = Math.Clamp(srsi[n - 1] / 100.0, 0, 1);

        // 2,3 MACD
        var (macd, sig, hist) = Trend.Macd(c);
        double macdScale = last * 0.01 + 1e-12;
        f[2] = Math.Clamp(hist[n - 1] / macdScale, -1, 1);
        f[3] = Math.Clamp((macd[n - 1] - sig[n - 1]) / macdScale, -1, 1);

        // 4,5 ADX
        var (adx, pdi, mdi) = Trend.Adx(h, l, c, 14);
        f[4] = double.IsNaN(adx[n - 1]) ? 0.5 : Math.Clamp(adx[n - 1] / 100.0, 0, 1);
        f[5] = Math.Clamp((pdi[n - 1] - mdi[n - 1]) / 100.0, -1, 1);

        // 6,7 Volatility Index + Drift Switch Index
        var vi = Volatility.VolatilityIndex(c, h, l);
        var dsi = Volatility.DriftSwitchIndex(c, h, l);
        f[6] = vi[n - 1];
        f[7] = dsi[n - 1];

        // 8 ATR%
        var atr = SeriesMath.Atr(h, l, c, 14);
        f[8] = Math.Clamp(atr[n - 1] / last, 0, 0.1);

        // 9 Bollinger %B
        var (bMid, bUp, bLo) = Volatility.Bollinger(c, 20, 2);
        double bRange = bUp[n - 1] - bLo[n - 1];
        f[9] = bRange < 1e-12 ? 0.5 : Math.Clamp((last - bLo[n - 1]) / bRange, 0, 1);

        // 10 ROC(10)
        var roc = Oscillators.Roc(c, 10);
        f[10] = Math.Clamp(roc[n - 1] / 5.0, -1, 1);

        // 11 CCI
        var cci = Oscillators.Cci(h, l, c, 20);
        f[11] = Math.Clamp(cci[n - 1] / 100.0, -1, 1);

        // 12 Williams %R
        var wr = Oscillators.WilliamsR(h, l, c, 14);
        f[12] = Math.Clamp(wr[n - 1] / 100.0, -1, 1);

        // 13 OBV slope
        var obv = SmartMoney.Obv(c, v);
        var obvSlope = Statistical.LinearRegression(obv, 20).Slope;
        f[13] = Math.Clamp(obvSlope / (obv.Max() - obv.Min() + 1e-12), -1, 1);

        // 14 Order-flow pressure
        var ofp = SmartMoney.OrderFlowPressure(c, v, 20);
        f[14] = ofp[n - 1];

        // 15 Price percentile rank
        f[15] = Statistical.PercentileRank(c, 100) / 100.0;

        // 16 Autocorrelation
        f[16] = Math.Clamp(Statistical.Autocorrelation(c, 1), -1, 1);

        // 17 Entropy
        f[17] = Statistical.Entropy(c, 10);

        // 18 Regression slope
        var slope = Statistical.LinearRegression(c, 30).Slope;
        f[18] = Math.Clamp(slope / (last * 0.005 + 1e-12), -1, 1);

        // 19 VWAP distance
        var vwap = SmartMoney.Vwap(h, l, c, v, 100);
        double vwapDist = vwap[n - 1] == 0 ? 0 : (last - vwap[n - 1]) / (atr[n - 1] + 1e-12);
        f[19] = Math.Clamp(vwapDist / 3.0, -1, 1);

        // 20-22 candle geometry
        var candle = candles[n - 1];
        f[20] = candle.Range < 1e-12 ? 0 : candle.Body / candle.Range;
        f[21] = candle.Range < 1e-12 ? 0 : candle.UpperWick / candle.Range;
        f[22] = candle.Range < 1e-12 ? 0 : candle.LowerWick / candle.Range;

        // 23 short-term momentum
        double mom5 = prev == 0 ? 0 : (last - prev) / prev;
        f[23] = Math.Clamp(mom5 / 0.02, -1, 1);

        return f;
    }

    /// <summary>Builds a labelled training sample: features + target (future direction of next k bars).</summary>
    public static (double[] Features, double Target) BuildTrainingSample(Candle[] candles, int lookAhead = 5, string symbol = "SYNTH")
    {
        var f = Extract(candles, symbol);
        int n = candles.Length;
        double future = candles[n - 1].Close;
        if (n - 1 + lookAhead < n)
            future = candles[n - 1 + lookAhead].Close;
        // If lookAhead candles aren't available, use the available tail
        int end = Math.Min(n - 1, n - 1);
        end = Math.Min(n - 1, candles.Length - 1);
        double futureClose = candles.Length > n - 1 + lookAhead ? candles[n - 1 + lookAhead].Close : candles[n - 1].Close;
        double pct = (futureClose - candles[n - 1].Close) / (candles[n - 1].Close + 1e-12);
        double target = Math.Clamp(pct / 0.01, -1, 1); // normalised to ~1% move = 1.0
        return (f, target);
    }
}
