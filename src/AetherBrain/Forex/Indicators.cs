namespace AetherBrain.Forex;

public static class Indicators
{
    public static double[] Rsi(IReadOnlyList<Candle> candles, int period = 14)
    {
        var result = Enumerable.Repeat(50d, candles.Count).ToArray();
        if (candles.Count <= period) return result;
        double gain = 0, loss = 0;
        for (var index = 1; index <= period; index++)
        {
            var change = candles[index].Close - candles[index - 1].Close;
            gain += Math.Max(0, change);
            loss += Math.Max(0, -change);
        }
        gain /= period;
        loss /= period;
        result[period] = ToRsi(gain, loss);
        for (var index = period + 1; index < candles.Count; index++)
        {
            var change = candles[index].Close - candles[index - 1].Close;
            gain = (gain * (period - 1) + Math.Max(0, change)) / period;
            loss = (loss * (period - 1) + Math.Max(0, -change)) / period;
            result[index] = ToRsi(gain, loss);
        }
        return result;
    }

    public static double[] Ema(IReadOnlyList<Candle> candles, int period)
    {
        var result = new double[candles.Count];
        if (candles.Count == 0) return result;
        var multiplier = 2d / (period + 1);
        result[0] = candles[0].Close;
        for (var index = 1; index < candles.Count; index++)
            result[index] = (candles[index].Close - result[index - 1]) * multiplier + result[index - 1];
        return result;
    }

    public static double Atr(IReadOnlyList<Candle> candles, int period = 14)
    {
        if (candles.Count < 2) return 0;
        return candles.Skip(Math.Max(1, candles.Count - period)).Select((candle, offset) =>
        {
            var index = Math.Max(1, candles.Count - period) + offset;
            var previousClose = candles[index - 1].Close;
            return Math.Max(candle.High - candle.Low, Math.Max(Math.Abs(candle.High - previousClose), Math.Abs(candle.Low - previousClose)));
        }).Average();
    }

    private static double ToRsi(double gain, double loss) => loss == 0 ? 100 : 100 - 100 / (1 + gain / loss);
}
