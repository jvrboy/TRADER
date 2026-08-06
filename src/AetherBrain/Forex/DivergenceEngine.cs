namespace AetherBrain.Forex;

public sealed class DivergenceEngine
{
    public IReadOnlyList<DivergenceSignal> Analyze(IReadOnlyList<Candle> candles, IReadOnlyList<double> oscillator, int lookback = 36)
    {
        if (candles.Count != oscillator.Count || candles.Count < 12) return [];
        var start = Math.Max(2, candles.Count - lookback);
        var lows = FindPivots(candles, start, false);
        var highs = FindPivots(candles, start, true);
        var signals = new List<DivergenceSignal>();
        Compare(lows, false, candles, oscillator, signals);
        Compare(highs, true, candles, oscillator, signals);
        return signals.OrderByDescending(signal => signal.Confidence).ToArray();
    }

    private static List<int> FindPivots(IReadOnlyList<Candle> candles, int start, bool high)
    {
        var pivots = new List<int>();
        for (var index = start; index < candles.Count - 2; index++)
        {
            var value = high ? candles[index].High : candles[index].Low;
            var isPivot = Enumerable.Range(index - 2, 5)
                .Where(candidate => candidate != index)
                .All(candidate => high ? value >= candles[candidate].High : value <= candles[candidate].Low);
            if (isPivot) pivots.Add(index);
        }
        return pivots;
    }

    private static void Compare(IReadOnlyList<int> pivots, bool highs, IReadOnlyList<Candle> candles, IReadOnlyList<double> oscillator, List<DivergenceSignal> signals)
    {
        if (pivots.Count < 2) return;
        var first = pivots[^2];
        var second = pivots[^1];
        var firstPrice = highs ? candles[first].High : candles[first].Low;
        var secondPrice = highs ? candles[second].High : candles[second].Low;
        var priceDelta = secondPrice - firstPrice;
        var oscillatorDelta = oscillator[second] - oscillator[first];
        var kind = (highs, priceDelta, oscillatorDelta) switch
        {
            (false, < 0, > 0) => DivergenceKind.RegularBullish,
            (true, > 0, < 0) => DivergenceKind.RegularBearish,
            (false, > 0, < 0) => DivergenceKind.HiddenBullish,
            (true, < 0, > 0) => DivergenceKind.HiddenBearish,
            _ => DivergenceKind.None
        };
        if (kind == DivergenceKind.None) return;
        var distance = Math.Abs(oscillatorDelta) / 100;
        var confidence = Math.Clamp(.52 + distance + Math.Min(.18, (second - first) / 100d), 0, .94);
        signals.Add(new DivergenceSignal(kind, "RSI-14", confidence, first, second,
            $"Price delta {priceDelta:F5}; oscillator delta {oscillatorDelta:F2}."));
    }
}
