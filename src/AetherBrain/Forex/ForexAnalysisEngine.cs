namespace AetherBrain.Forex;

public sealed class ForexAnalysisEngine
{
    private readonly DivergenceEngine _divergence = new();

    public ForexAnalysis Analyze(string symbol, IReadOnlyList<Candle> candles)
    {
        if (candles.Count < 30) throw new ArgumentException("At least 30 candles are required.", nameof(candles));
        var rsi = Indicators.Rsi(candles);
        var fast = Indicators.Ema(candles, 12);
        var slow = Indicators.Ema(candles, 26);
        var lastClose = candles[^1].Close;
        var atr = Indicators.Atr(candles);
        var momentum = lastClose == 0 ? 0 : (fast[^1] - slow[^1]) / lastClose * 100;
        var volatility = lastClose == 0 ? 0 : atr / lastClose * 100;
        var divergences = _divergence.Analyze(candles, rsi);
        var regime = Math.Abs(momentum) < .03 ? "Compression" : momentum > 0 ? "Bullish expansion" : "Bearish expansion";
        var risk = Math.Clamp(volatility * 24 + divergences.Count * 7, 0, 100);
        var narrative = divergences.Count == 0
            ? $"{regime}; no confirmed RSI pivot divergence in the active window."
            : $"{regime}; strongest signal is {divergences[0].Kind} at {divergences[0].Confidence:P0} confidence.";
        return new ForexAnalysis(symbol, regime, momentum, volatility, risk, divergences, narrative);
    }
}
