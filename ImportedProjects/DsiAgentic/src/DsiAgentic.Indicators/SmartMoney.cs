using DsiAgentic.Core;

namespace DsiAgentic.Indicators;

/// <summary>
/// Smart Money Concepts: Order Blocks, Fair Value Gaps, Liquidity Sweeps,
/// Break of Structure, Change of Character, Premium/Discount zones.
/// </summary>
public static class SmartMoney
{
    public sealed record OrderBlock(int Index, double High, double Low, bool Bullish);
    public sealed record Fvg(int Index, double Upper, double Lower, bool Bullish);
    public sealed record Sweep(int Index, double Price, bool Bullish);
    public sealed record StructurePoint(int Index, double Price, string Type); // BOS_UP, BOS_DN, CHOCH_UP, CHOCH_DN

    public static List<OrderBlock> DetectOrderBlocks(Series s, int lookback = 50, int impulseBars = 3, double impulseAtrMult = 1.5)
    {
        var atr = SeriesMath.Atr(s.High, s.Low, s.Close, 14);
        var list = new List<OrderBlock>();
        int start = Math.Max(3, s.Count - lookback);
        for (int i = start; i < s.Count - impulseBars; i++)
        {
            var impulse = s.Close[i + impulseBars] - s.Close[i];
            if (double.IsNaN(atr[i]) || atr[i] == 0) continue;
            if (Math.Abs(impulse) < impulseAtrMult * atr[i]) continue;
            bool bull = impulse > 0;
            // last opposite candle before impulse = order block
            for (int j = i; j >= Math.Max(0, i - 5); j--)
            {
                bool isDown = s.Close[j] < s.Open[j];
                if (bull == !isDown) continue; // want opposite candle
                list.Add(new OrderBlock(j, s.High[j], s.Low[j], bull));
                break;
            }
        }
        return list;
    }

    public static List<Fvg> DetectFvgs(Series s, int lookback = 80)
    {
        var list = new List<Fvg>();
        int start = Math.Max(2, s.Count - lookback);
        for (int i = start; i < s.Count; i++)
        {
            // bullish FVG: low[i] > high[i-2]
            if (s.Low[i] > s.High[i - 2])
                list.Add(new Fvg(i, s.Low[i], s.High[i - 2], true));
            else if (s.High[i] < s.Low[i - 2])
                list.Add(new Fvg(i, s.Low[i - 2], s.High[i], false));
        }
        return list;
    }

    public static List<Sweep> DetectLiquiditySweeps(Series s, int lookback = 80, int refLen = 20)
    {
        var list = new List<Sweep>();
        int start = Math.Max(refLen, s.Count - lookback);
        for (int i = start; i < s.Count; i++)
        {
            double refHigh = double.MinValue, refLow = double.MaxValue;
            for (int j = i - refLen; j < i; j++) { if (s.High[j] > refHigh) refHigh = s.High[j]; if (s.Low[j] < refLow) refLow = s.Low[j]; }
            if (s.High[i] > refHigh && s.Close[i] < refHigh) list.Add(new Sweep(i, s.High[i], false));
            else if (s.Low[i] < refLow && s.Close[i] > refLow) list.Add(new Sweep(i, s.Low[i], true));
        }
        return list;
    }

    public static (double premium, double discount, double equilibrium) PremiumDiscount(Series s, int lookback = 50)
    {
        int start = Math.Max(0, s.Count - lookback);
        double hh = double.MinValue, ll = double.MaxValue;
        for (int i = start; i < s.Count; i++) { if (s.High[i] > hh) hh = s.High[i]; if (s.Low[i] < ll) ll = s.Low[i]; }
        double eq = (hh + ll) / 2;
        return (hh, ll, eq);
    }

    public static List<StructurePoint> DetectStructure(Series s, int pivotStrength = 3)
    {
        var list = new List<StructurePoint>();
        int n = s.Count;
        double lastHigh = double.NaN, lastLow = double.NaN;
        for (int i = pivotStrength; i < n - pivotStrength; i++)
        {
            bool isHigh = true, isLow = true;
            for (int k = 1; k <= pivotStrength; k++)
            {
                if (s.High[i - k] >= s.High[i] || s.High[i + k] >= s.High[i]) isHigh = false;
                if (s.Low[i - k] <= s.Low[i] || s.Low[i + k] <= s.Low[i]) isLow = false;
            }
            if (isHigh)
            {
                if (!double.IsNaN(lastHigh) && s.High[i] > lastHigh)
                    list.Add(new StructurePoint(i, s.High[i], "BOS_UP"));
                lastHigh = s.High[i];
            }
            if (isLow)
            {
                if (!double.IsNaN(lastLow) && s.Low[i] < lastLow)
                    list.Add(new StructurePoint(i, s.Low[i], "BOS_DN"));
                lastLow = s.Low[i];
            }
        }
        return list;
    }
}
