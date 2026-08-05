using DsiAgentic.Core;
using DsiAgentic.Indicators;

namespace DsiAgentic.Strategies;

public sealed class TrendRibbonStrategy : IStrategy
{
    public string Name => "trend_ribbon";
    public string Family => "trend";
    public double Weight => 1.5;
    public Vote? Evaluate(Series s)
    {
        if (s.Count < 220) return null;
        var ribbon = Trend.Ribbon(s.Close, new[] { 8, 13, 21, 34, 55 });
        int bull = 0, bear = 0;
        for (int i = 0; i < ribbon.Length - 1; i++)
        {
            if (ribbon[i][^1] > ribbon[i + 1][^1]) bull++; else bear++;
        }
        var dir = bull > bear ? Direction.Buy : bear > bull ? Direction.Sell : Direction.Neutral;
        if (dir == Direction.Neutral) return null;
        return new Vote { Agent = Name, Family = Family, Direction = dir, Weight = Weight, Confidence = 0.7, Reason = $"ribbon bull={bull} bear={bear}" };
    }
}

public sealed class AdxTrendStrategy : IStrategy
{
    public string Name => "adx_trend";
    public string Family => "trend";
    public double Weight => 1.0;
    public Vote? Evaluate(Series s)
    {
        if (s.Count < 60) return null;
        var (adx, pdi, mdi) = Trend.Adx(s.High, s.Low, s.Close, 14);
        if (adx[^1] < 20) return null;
        var dir = pdi[^1] > mdi[^1] ? Direction.Buy : Direction.Sell;
        return new Vote { Agent = Name, Family = Family, Direction = dir, Weight = Weight, Confidence = Math.Min(1, adx[^1] / 50), Reason = $"adx={adx[^1]:F1}" };
    }
}

public sealed class SuperTrendStrategy : IStrategy
{
    public string Name => "supertrend";
    public string Family => "trend";
    public double Weight => 1.0;
    public Vote? Evaluate(Series s)
    {
        if (s.Count < 60) return null;
        var (_, dir) = Trend.SuperTrend(s.High, s.Low, s.Close);
        var d = dir[^1] == 1 ? Direction.Buy : Direction.Sell;
        return new Vote { Agent = Name, Family = Family, Direction = d, Weight = Weight, Confidence = 0.6, Reason = $"st_dir={dir[^1]}" };
    }
}

public sealed class IchimokuStrategy : IStrategy
{
    public string Name => "ichimoku";
    public string Family => "trend";
    public double Weight => 1.25;
    public Vote? Evaluate(Series s)
    {
        if (s.Count < 60) return null;
        var (conv, baseL, sA, sB) = Trend.Ichimoku(s.High, s.Low);
        var c = s.Close[^1];
        bool above = c > sA[^1] && c > sB[^1];
        bool below = c < sA[^1] && c < sB[^1];
        if (above) return new Vote { Agent = Name, Family = Family, Direction = Direction.Buy, Weight = Weight, Confidence = 0.7, Reason = "above_cloud" };
        if (below) return new Vote { Agent = Name, Family = Family, Direction = Direction.Sell, Weight = Weight, Confidence = 0.7, Reason = "below_cloud" };
        return null;
    }
}

public sealed class RsiExtremeStrategy : IStrategy
{
    public string Name => "rsi_extreme";
    public string Family => "momentum";
    public double Weight => 0.8;
    public Vote? Evaluate(Series s)
    {
        if (s.Count < 30) return null;
        var r = Oscillators.Rsi(s.Close, 14)[^1];
        if (r < 25) return new Vote { Agent = Name, Family = Family, Direction = Direction.Buy, Weight = Weight, Confidence = (30 - r) / 30, Reason = $"rsi={r:F1}" };
        if (r > 75) return new Vote { Agent = Name, Family = Family, Direction = Direction.Sell, Weight = Weight, Confidence = (r - 70) / 30, Reason = $"rsi={r:F1}" };
        return null;
    }
}

public sealed class MacdCrossStrategy : IStrategy
{
    public string Name => "macd_cross";
    public string Family => "momentum";
    public double Weight => 1.0;
    public Vote? Evaluate(Series s)
    {
        if (s.Count < 60) return null;
        var (m, sig, hist) = Oscillators.Macd(s.Close);
        if (hist[^1] > 0 && hist[^2] <= 0) return new Vote { Agent = Name, Family = Family, Direction = Direction.Buy, Weight = Weight, Confidence = 0.7, Reason = "hist_flip_up" };
        if (hist[^1] < 0 && hist[^2] >= 0) return new Vote { Agent = Name, Family = Family, Direction = Direction.Sell, Weight = Weight, Confidence = 0.7, Reason = "hist_flip_dn" };
        return null;
    }
}

public sealed class BollingerReversionStrategy : IStrategy
{
    public string Name => "bb_reversion";
    public string Family => "volatility";
    public double Weight => 0.75;
    public Vote? Evaluate(Series s)
    {
        if (s.Count < 30) return null;
        var (up, mid, lo) = Volatility.Bollinger(s.Close);
        var c = s.Close[^1];
        if (c < lo[^1]) return new Vote { Agent = Name, Family = Family, Direction = Direction.Buy, Weight = Weight, Confidence = 0.6, Reason = "below_lower" };
        if (c > up[^1]) return new Vote { Agent = Name, Family = Family, Direction = Direction.Sell, Weight = Weight, Confidence = 0.6, Reason = "above_upper" };
        return null;
    }
}

public sealed class DonchianBreakoutStrategy : IStrategy
{
    public string Name => "donchian_break";
    public string Family => "structure";
    public double Weight => 1.0;
    public Vote? Evaluate(Series s)
    {
        if (s.Count < 30) return null;
        var (up, mid, lo) = Trend.Donchian(s.High, s.Low, 20);
        var c = s.Close[^1];
        if (c >= up[^2]) return new Vote { Agent = Name, Family = Family, Direction = Direction.Buy, Weight = Weight, Confidence = 0.7, Reason = "break_up" };
        if (c <= lo[^2]) return new Vote { Agent = Name, Family = Family, Direction = Direction.Sell, Weight = Weight, Confidence = 0.7, Reason = "break_dn" };
        return null;
    }
}

public sealed class KeltnerSqueezeStrategy : IStrategy
{
    public string Name => "keltner_squeeze";
    public string Family => "volatility";
    public double Weight => 0.6;
    public Vote? Evaluate(Series s)
    {
        if (s.Count < 30) return null;
        var (bu, bm, bl) = Volatility.Bollinger(s.Close);
        var (ku, km, kl) = Trend.Keltner(s.High, s.Low, s.Close);
        bool squeeze = bu[^1] < ku[^1] && bl[^1] > kl[^1];
        if (!squeeze) return null;
        double slope = SeriesMath.LinRegSlope(s.Close, 10);
        var dir = slope > 0 ? Direction.Buy : Direction.Sell;
        return new Vote { Agent = Name, Family = Family, Direction = dir, Weight = Weight, Confidence = 0.55, Reason = "squeeze_release" };
    }
}

public sealed class StochCrossStrategy : IStrategy
{
    public string Name => "stoch_cross";
    public string Family => "momentum";
    public double Weight => 0.5;
    public Vote? Evaluate(Series s)
    {
        if (s.Count < 30) return null;
        var (k, d) = Oscillators.Stochastic(s.High, s.Low, s.Close);
        if (k[^2] < d[^2] && k[^1] > d[^1] && k[^1] < 30) return new Vote { Agent = Name, Family = Family, Direction = Direction.Buy, Weight = Weight, Confidence = 0.6, Reason = "cross_up_os" };
        if (k[^2] > d[^2] && k[^1] < d[^1] && k[^1] > 70) return new Vote { Agent = Name, Family = Family, Direction = Direction.Sell, Weight = Weight, Confidence = 0.6, Reason = "cross_dn_ob" };
        return null;
    }
}

public sealed class CciTrendStrategy : IStrategy
{
    public string Name => "cci_trend";
    public string Family => "momentum";
    public double Weight => 0.6;
    public Vote? Evaluate(Series s)
    {
        if (s.Count < 30) return null;
        var cci = Oscillators.Cci(s.High, s.Low, s.Close);
        if (cci[^1] > 100 && cci[^2] <= 100) return new Vote { Agent = Name, Family = Family, Direction = Direction.Buy, Weight = Weight, Confidence = 0.6, Reason = "cci_up" };
        if (cci[^1] < -100 && cci[^2] >= -100) return new Vote { Agent = Name, Family = Family, Direction = Direction.Sell, Weight = Weight, Confidence = 0.6, Reason = "cci_dn" };
        return null;
    }
}

public sealed class SmcOrderBlockStrategy : IStrategy
{
    public string Name => "smc_ob";
    public string Family => "smc";
    public double Weight => 1.2;
    public Vote? Evaluate(Series s)
    {
        if (s.Count < 50) return null;
        var obs = SmartMoney.DetectOrderBlocks(s);
        if (obs.Count == 0) return null;
        var last = obs[^1];
        var c = s.Close[^1];
        bool inZone = c >= last.Low && c <= last.High;
        if (!inZone) return null;
        return new Vote { Agent = Name, Family = Family, Direction = last.Bullish ? Direction.Buy : Direction.Sell, Weight = Weight, Confidence = 0.75, Reason = "in_ob" };
    }
}

public sealed class FvgFillStrategy : IStrategy
{
    public string Name => "fvg_fill";
    public string Family => "smc";
    public double Weight => 1.0;
    public Vote? Evaluate(Series s)
    {
        if (s.Count < 50) return null;
        var gaps = SmartMoney.DetectFvgs(s);
        if (gaps.Count == 0) return null;
        var g = gaps[^1];
        var c = s.Close[^1];
        if (c >= g.Lower && c <= g.Upper)
            return new Vote { Agent = Name, Family = Family, Direction = g.Bullish ? Direction.Buy : Direction.Sell, Weight = Weight, Confidence = 0.7, Reason = "fvg_retest" };
        return null;
    }
}

public sealed class SweepReversalStrategy : IStrategy
{
    public string Name => "liquidity_sweep";
    public string Family => "smc";
    public double Weight => 1.1;
    public Vote? Evaluate(Series s)
    {
        if (s.Count < 40) return null;
        var sw = SmartMoney.DetectLiquiditySweeps(s);
        if (sw.Count == 0) return null;
        var last = sw[^1];
        if (s.Count - 1 - last.Index > 3) return null;
        return new Vote { Agent = Name, Family = Family, Direction = last.Bullish ? Direction.Buy : Direction.Sell, Weight = Weight, Confidence = 0.8, Reason = "sweep_reversal" };
    }
}
