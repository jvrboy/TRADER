using NexusBrain.Core;
using NexusBrain.Divergence;
using NexusBrain.Indicators;

namespace NexusBrain.Agents;

/// <summary>Momentum agent: rides strong trends detected by ADX/MACD/ROC.</summary>
public sealed class MomentumAgent : SubAgentBase
{
    public override string Name => "momentum";
    public override string Description => "Detects strong directional momentum via ADX, MACD and ROC.";
    public override double Weight => 1.0;

    public override IEnumerable<Signal> Analyze(Candle[] candles, string symbol, InstrumentKind kind)
    {
        if (candles.Length < 40) yield break;
        var n = candles.Length;
        var c = new double[n]; var h = new double[n]; var l = new double[n];
        for (int i = 0; i < n; i++) { c[i] = candles[i].Close; h[i] = candles[i].High; l[i] = candles[i].Low; }

        var (adx, pdi, mdi) = Trend.Adx(h, l, c, 14);
        var (macd, sig, hist) = Trend.Macd(c);
        var roc = Oscillators.Roc(c, 10);

        double adxV = double.IsNaN(adx[n - 1]) ? 0 : adx[n - 1];
        double score = 0;
        if (adxV > 25 && pdi[n - 1] > mdi[n - 1]) score += 1;
        if (adxV > 25 && mdi[n - 1] > pdi[n - 1]) score -= 1;
        if (hist[n - 1] > 0 && hist[n - 1] > hist[n - 2]) score += 0.5;
        if (hist[n - 1] < 0 && hist[n - 1] < hist[n - 2]) score -= 0.5;
        if (roc[n - 1] > 1) score += 0.5;
        if (roc[n - 1] < -1) score -= 0.5;

        if (score >= 1)
        {
            double conf = Math.Clamp(0.4 + adxV / 150.0, 0, 1);
            yield return new Signal
            {
                Symbol = symbol, Bias = Bias.Bullish, Confidence = conf,
                Strength = score, Agent = Name,
                Reason = $"Strong uptrend (ADX {adxV:F0}, +DI>{pdi[n - 1]:F0}>-DI {mdi[n - 1]:F0}, MACD rising)",
                Epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }
        else if (score <= -1)
        {
            double conf = Math.Clamp(0.4 + adxV / 150.0, 0, 1);
            yield return new Signal
            {
                Symbol = symbol, Bias = Bias.Bearish, Confidence = conf,
                Strength = score, Agent = Name,
                Reason = $"Strong downtrend (ADX {adxV:F0}, -DI>{mdi[n - 1]:F0}>+DI {pdi[n - 1]:F0}, MACD falling)",
                Epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }
    }
}

/// <summary>Mean-reversion agent: fades overbought/oversold extremes.</summary>
public sealed class MeanReversionAgent : SubAgentBase
{
    public override string Name => "mean_reversion";
    public override string Description => "Fades RSI/Stochastic extremes when no strong trend is present.";
    public override double Weight => 0.8;

    public override IEnumerable<Signal> Analyze(Candle[] candles, string symbol, InstrumentKind kind)
    {
        if (candles.Length < 40) yield break;
        var n = candles.Length;
        var c = new double[n]; var h = new double[n]; var l = new double[n];
        for (int i = 0; i < n; i++) { c[i] = candles[i].Close; h[i] = candles[i].High; l[i] = candles[i].Low; }

        var rsi = Oscillators.Rsi(c, 14);
        var (k, d) = Oscillators.Stochastic(h, l, c);
        var (adx, _, _) = Trend.Adx(h, l, c, 14);
        double adxV = double.IsNaN(adx[n - 1]) ? 0 : adx[n - 1];

        // Only fade when NOT in a strong trend
        if (adxV > 30) yield break;

        if (rsi[n - 1] < 30 && k[n - 1] < 20)
        {
            yield return new Signal
            {
                Symbol = symbol, Bias = Bias.Bullish, Confidence = 0.6,
                Strength = (30 - rsi[n - 1]) / 30, Agent = Name,
                Reason = $"Oversold bounce (RSI {rsi[n - 1]:F0}, Stoch {k[n - 1]:F0})",
                Epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }
        else if (rsi[n - 1] > 70 && k[n - 1] > 80)
        {
            yield return new Signal
            {
                Symbol = symbol, Bias = Bias.Bearish, Confidence = 0.6,
                Strength = (rsi[n - 1] - 70) / 30, Agent = Name,
                Reason = $"Overbought pullback (RSI {rsi[n - 1]:F0}, Stoch {k[n - 1]:F0})",
                Epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }
    }
}

/// <summary>Divergence agent: trades detected price/oscillator divergences.</summary>
public sealed class DivergenceAgent : SubAgentBase
{
    public override string Name => "divergence";
    public override string Description => "Trades regular and hidden divergences between price and RSI/MACD/StochRSI.";
    public override double Weight => 1.2;

    public override IEnumerable<Signal> Analyze(Candle[] candles, string symbol, InstrumentKind kind)
    {
        if (candles.Length < 40) yield break;
        var divs = DivergenceEngine.Scan(candles, symbol);
        var (aggType, aggStrength) = DivergenceEngine.Aggregate(divs);
        if (aggType == DivergenceType.None) yield break;

        var strongest = divs.OrderByDescending(d => d.Strength).First();
        bool bullish = aggType is DivergenceType.BullishRegular or DivergenceType.BullishHidden;
        yield return new Signal
        {
            Symbol = symbol,
            Bias = bullish ? Bias.Bullish : Bias.Bearish,
            Confidence = Math.Clamp(0.5 + aggStrength * 0.4, 0, 0.95),
            Strength = aggStrength,
            Agent = Name,
            Reason = $"{strongest.Type} divergence on {strongest.Indicator} (strength {aggStrength:P0})",
            Epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Metrics = divs.ToDictionary(d => d.Indicator, d => d.Strength)
        };
    }
}

/// <summary>Volatility agent: adapts positioning to the Volatility Index regime.</summary>
public sealed class VolatilityAgent : SubAgentBase
{
    public override string Name => "volatility";
    public override string Description => "Assesses the Volatility Index regime and adjusts risk posture.";
    public override double Weight => 0.9;

    public override IEnumerable<Signal> Analyze(Candle[] candles, string symbol, InstrumentKind kind)
    {
        if (candles.Length < 40) yield break;
        var n = candles.Length;
        var c = new double[n]; var h = new double[n]; var l = new double[n];
        for (int i = 0; i < n; i++) { c[i] = candles[i].Close; h[i] = candles[i].High; l[i] = candles[i].Low; }

        var vi = Volatility.VolatilityIndex(c, h, l);
        double v = vi[n - 1];
        var regime = Volatility.RegimeLabel(v);

        // In high vol, trend-following is favoured; in low vol, range trades.
        var (adx, pdi, mdi) = Trend.Adx(h, l, c, 14);
        double adxV = double.IsNaN(adx[n - 1]) ? 0 : adx[n - 1];

        if (v > 0.6 && adxV > 25)
        {
            bool up = pdi[n - 1] > mdi[n - 1];
            yield return new Signal
            {
                Symbol = symbol, Bias = up ? Bias.Bullish : Bias.Bearish,
                Confidence = Math.Clamp(0.5 + v * 0.3, 0, 0.9),
                Strength = v, Agent = Name,
                Reason = $"{regime} + trend → favour {(up ? "longs" : "shorts")} (VI {v:P0})",
                Epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Metrics = new Dictionary<string, double> { ["vi"] = v }
            };
        }
        else if (v < 0.35)
        {
            yield return new Signal
            {
                Symbol = symbol, Bias = Bias.Neutral,
                Confidence = 0.5, Strength = -v, Agent = Name,
                Reason = $"{regime} → expect range, tighten stops (VI {v:P0})",
                Epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Metrics = new Dictionary<string, double> { ["vi"] = v }
            };
        }
    }
}

/// <summary>Drift Switch agent: trades the Drift Switch Index trending/ranging regime.</summary>
public sealed class DriftSwitchAgent : SubAgentBase
{
    public override string Name => "drift_switch";
    public override string Description => "Uses the Drift Switch Index to switch between trend and range strategies.";
    public override double Weight => 1.0;

    public override IEnumerable<Signal> Analyze(Candle[] candles, string symbol, InstrumentKind kind)
    {
        if (candles.Length < 40) yield break;
        var n = candles.Length;
        var c = new double[n]; var h = new double[n]; var l = new double[n];
        for (int i = 0; i < n; i++) { c[i] = candles[i].Close; h[i] = candles[i].High; l[i] = candles[i].Low; }

        var dsi = Volatility.DriftSwitchIndex(c, h, l);
        double d = dsi[n - 1];
        var label = Volatility.DriftLabel(d);

        var (adx, pdi, mdi) = Trend.Adx(h, l, c, 14);
        double adxV = double.IsNaN(adx[n - 1]) ? 0 : adx[n - 1];

        if (d > 0.65)
        {
            // Trending regime — follow the trend
            bool up = pdi[n - 1] > mdi[n - 1] || c[n - 1] > c[n - 2];
            yield return new Signal
            {
                Symbol = symbol, Bias = up ? Bias.Bullish : Bias.Bearish,
                Confidence = Math.Clamp(0.5 + d * 0.3, 0, 0.9),
                Strength = d, Agent = Name,
                Reason = $"DSI {label} ({d:P0}) → trend-follow {(up ? "long" : "short")}",
                Epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Metrics = new Dictionary<string, double> { ["dsi"] = d }
            };
        }
        else if (d < 0.4)
        {
            yield return new Signal
            {
                Symbol = symbol, Bias = Bias.Neutral,
                Confidence = 0.5, Strength = -d, Agent = Name,
                Reason = $"DSI {label} ({d:P0}) → range-bound, avoid chasing",
                Epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Metrics = new Dictionary<string, double> { ["dsi"] = d }
            };
        }
    }
}

/// <summary>Forex agent: full forex analysis (pivots, S/R, patterns, risk-reward).</summary>
public sealed class ForexAgent : SubAgentBase
{
    public override string Name => "forex";
    public override string Description => "Runs the full forex analysis system (pivots, S/R, Fibonacci, patterns).";
    public override double Weight => 1.1;

    public override IEnumerable<Signal> Analyze(Candle[] candles, string symbol, InstrumentKind kind)
    {
        if (kind != InstrumentKind.Forex || candles.Length < 30) yield break;
        var fa = Forex.ForexAnalyzer.Analyze(candles, symbol);
        if (fa.Bias == Bias.Neutral) yield break;

        double entry = fa.LastPrice;
        double stop = fa.Bias == Bias.Bullish
            ? entry - fa.SuggestedStopPips * fa.PipSize
            : entry + fa.SuggestedStopPips * fa.PipSize;
        double target = fa.Bias == Bias.Bullish
            ? entry + fa.SuggestedTargetPips * fa.PipSize
            : entry - fa.SuggestedTargetPips * fa.PipSize;

        yield return new Signal
        {
            Symbol = symbol, Bias = fa.Bias, Confidence = fa.Confidence,
            Strength = fa.Confidence, Agent = Name,
            Reason = string.Join("; ", fa.Notes),
            Entry = entry.ToString("F5"), Stop = stop.ToString("F5"), Target = target.ToString("F5"),
            Epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Metrics = new Dictionary<string, double>
            {
                ["rr"] = fa.RiskReward, ["atr_pips"] = fa.AtrPips, ["pivot"] = fa.Pivots.P
            }
        };
    }
}

/// <summary>Trend agent: multi-timeframe trend alignment via MAs and SuperTrend.</summary>
public sealed class TrendAgent : SubAgentBase
{
    public override string Name => "trend";
    public override string Description => "Confirms trend via EMA alignment, SuperTrend and price vs VWAP.";
    public override double Weight => 0.9;

    public override IEnumerable<Signal> Analyze(Candle[] candles, string symbol, InstrumentKind kind)
    {
        if (candles.Length < 60) yield break;
        var n = candles.Length;
        var c = new double[n]; var h = new double[n]; var l = new double[n];
        for (int i = 0; i < n; i++) { c[i] = candles[i].Close; h[i] = candles[i].High; l[i] = candles[i].Low; }

        var ema20 = SeriesMath.Ema(c, 20);
        var ema50 = SeriesMath.Ema(c, 50);
        var ema200 = SeriesMath.Ema(c, 200);
        var st = Trend.SuperTrend(h, l, c, 10, 3);

        double score = 0;
        if (c[n - 1] > ema20[n - 1] && ema20[n - 1] > ema50[n - 1]) score += 1;
        else if (c[n - 1] < ema20[n - 1] && ema20[n - 1] < ema50[n - 1]) score -= 1;
        if (ema50[n - 1] > ema200[n - 1]) score += 0.5; else score -= 0.5;
        if (c[n - 1] > st[n - 1]) score += 0.5; else score -= 0.5;

        if (score >= 1.5)
            yield return new Signal
            {
                Symbol = symbol, Bias = Bias.Bullish, Confidence = 0.65, Strength = score,
                Agent = Name, Reason = "Bullish MA alignment + SuperTrend long",
                Epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        else if (score <= -1.5)
            yield return new Signal
            {
                Symbol = symbol, Bias = Bias.Bearish, Confidence = 0.65, Strength = score,
                Agent = Name, Reason = "Bearish MA alignment + SuperTrend short",
                Epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
    }
}
