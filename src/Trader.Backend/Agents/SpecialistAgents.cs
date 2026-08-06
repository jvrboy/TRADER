using Trader.Backend.Core;

namespace Trader.Backend.Agents;

/// <summary>
/// Trend-following agent. Votes with the direction of the EMA trend and ADX
/// strength; abstains (neutral) when the market is flat.
/// </summary>
public sealed class TrendSwarmAgent : ISwarmAgent
{
    public string Name => "TrendAgent";
    public string Family => "trend";
    public double Weight => 1.0;

    public SwarmVote Evaluate(IReadOnlyList<CandleData> market)
    {
        var closes = market.Select(c => c.Close).ToArray();
        if (closes.Length < 40)
            return new SwarmVote(Name, Family, VoteDirection.Neutral, 0, Weight);

        var emaFast = Ema(closes, 9);
        var emaSlow = Ema(closes, 21);

        var fast = emaFast[^1];
        var slow = emaSlow[^1];
        var gap = Math.Abs(fast - slow) / slow;

        if (gap < 0.0005) // flat -> abstain
            return new SwarmVote(Name, Family, VoteDirection.Neutral, 0, Weight);

        var dir = fast > slow ? VoteDirection.Buy : VoteDirection.Sell;
        var confidence = Math.Clamp(gap / 0.005, 0.1, 0.95);
        return new SwarmVote(Name, Family, dir, confidence, Weight);
    }

    private static double[] Ema(double[] closes, int period)
    {
        var k = 2.0 / (period + 1);
        var ema = new double[closes.Length];
        ema[0] = closes[0];
        for (var i = 1; i < closes.Length; i++)
            ema[i] = closes[i] * k + ema[i - 1] * (1 - k);
        return ema;
    }
}

/// <summary>
/// Momentum agent. Votes with RSI + ROC momentum; strong momentum in one
/// direction raises confidence.
/// </summary>
public sealed class MomentumSwarmAgent : ISwarmAgent
{
    public string Name => "MomentumAgent";
    public string Family => "momentum";
    public double Weight => 0.9;

    public SwarmVote Evaluate(IReadOnlyList<CandleData> market)
    {
        var closes = market.Select(c => c.Close).ToArray();
        if (closes.Length < 30)
            return new SwarmVote(Name, Family, VoteDirection.Neutral, 0, Weight);

        var rsi = Rsi(closes, 14);
        var roc = closes.Length > 12 ? (closes[^1] / closes[closes.Length - 13] - 1.0) * 100.0 : 0;

        var score = (rsi - 50) * 0.8 + roc * 3.0;
        if (Math.Abs(score) < 8)
            return new SwarmVote(Name, Family, VoteDirection.Neutral, 0, Weight);

        var dir = score > 0 ? VoteDirection.Buy : VoteDirection.Sell;
        var confidence = Math.Clamp(Math.Abs(score) / 40.0, 0.1, 0.95);
        return new SwarmVote(Name, Family, dir, confidence, Weight);
    }

    private static double Rsi(double[] closes, int period)
    {
        if (closes.Length < period + 1) return 50;
        double gain = 0, loss = 0;
        for (var i = closes.Length - period; i < closes.Length - 1; i++)
        {
            var d = closes[i + 1] - closes[i];
            if (d >= 0) gain += d; else loss -= d;
        }
        if (loss == 0) return 100;
        return 100 - 100 / (1 + (gain / period) / (loss / period));
    }
}

/// <summary>
/// Volatility agent. Votes mean-reversion when price is stretched beyond
/// Bollinger bands; abstains in quiet conditions.
/// </summary>
public sealed class VolatilitySwarmAgent : ISwarmAgent
{
    public string Name => "VolatilityAgent";
    public string Family => "volatility";
    public double Weight => 0.7;

    public SwarmVote Evaluate(IReadOnlyList<CandleData> market)
    {
        var closes = market.Select(c => c.Close).ToArray();
        if (closes.Length < 30)
            return new SwarmVote(Name, Family, VoteDirection.Neutral, 0, Weight);

        var period = 20;
        var mean = closes[^period..].Average();
        var std = Math.Sqrt(closes[^period..].Sum(c => (c - mean) * (c - mean)) / period);
        if (std <= 0)
            return new SwarmVote(Name, Family, VoteDirection.Neutral, 0, Weight);

        var z = (closes[^1] - mean) / std;
        if (Math.Abs(z) < 1.5) // within bands -> abstain
            return new SwarmVote(Name, Family, VoteDirection.Neutral, 0, Weight);

        // Price stretched above upper band -> expect reversion down (sell)
        var dir = z > 0 ? VoteDirection.Sell : VoteDirection.Buy;
        var confidence = Math.Clamp((Math.Abs(z) - 1.5) / 2.0, 0.1, 0.9);
        return new SwarmVote(Name, Family, dir, confidence, Weight);
    }
}

/// <summary>
/// Breakout agent. Votes when price breaks out of a recent range with rising
/// volume; direction follows the breakout.
/// </summary>
public sealed class BreakoutSwarmAgent : ISwarmAgent
{
    public string Name => "BreakoutAgent";
    public string Family => "breakout";
    public double Weight => 0.8;

    public SwarmVote Evaluate(IReadOnlyList<CandleData> market)
    {
        var candles = market.ToArray();
        if (candles.Length < 30)
            return new SwarmVote(Name, Family, VoteDirection.Neutral, 0, Weight);

        var lookback = 20;
        var prior = candles[^lookback..^1];
        var rangeHigh = prior.Max(c => c.High);
        var rangeLow = prior.Min(c => c.Low);
        var last = candles[^1];

        var range = rangeHigh - rangeLow;
        if (range <= 0)
            return new SwarmVote(Name, Family, VoteDirection.Neutral, 0, Weight);

        // Volume confirmation: last bar above the prior average
        var avgVol = prior.Average(c => c.Volume);
        var volConfirmed = avgVol > 0 && last.Volume > avgVol * 1.1;

        if (last.Close > rangeHigh && volConfirmed)
            return new SwarmVote(Name, Family, VoteDirection.Buy, 0.7, Weight);
        if (last.Close < rangeLow && volConfirmed)
            return new SwarmVote(Name, Family, VoteDirection.Sell, 0.7, Weight);

        return new SwarmVote(Name, Family, VoteDirection.Neutral, 0, Weight);
    }
}

/// <summary>
/// Volume agent. Votes based on volume-price confirmation (rising volume
/// supporting the price direction).
/// </summary>
public sealed class VolumeSwarmAgent : ISwarmAgent
{
    public string Name => "VolumeAgent";
    public string Family => "volume";
    public double Weight => 0.6;

    public SwarmVote Evaluate(IReadOnlyList<CandleData> market)
    {
        var candles = market.ToArray();
        if (candles.Length < 25)
            return new SwarmVote(Name, Family, VoteDirection.Neutral, 0, Weight);

        var last = candles[^1];
        var prior = candles[^20..^1];
        var avgVol = prior.Average(c => c.Volume);
        if (avgVol <= 0)
            return new SwarmVote(Name, Family, VoteDirection.Neutral, 0, Weight);

        var relVol = last.Volume / avgVol;
        var priceUp = last.Close > candles[^2].Close;

        if (relVol < 1.1)
            return new SwarmVote(Name, Family, VoteDirection.Neutral, 0, Weight);

        var dir = priceUp ? VoteDirection.Buy : VoteDirection.Sell;
        var confidence = Math.Clamp((relVol - 1.0) / 1.5, 0.1, 0.9);
        return new SwarmVote(Name, Family, dir, confidence, Weight);
    }
}
