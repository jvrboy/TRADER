using DsiAgentic.Core;
using DsiAgentic.Indicators;

namespace DsiAgentic.Divergence;

/// <summary>
/// Detects all 4 divergence types (regular bull/bear, hidden bull/bear) across
/// 12+ oscillators and scores each 0-100 with grade A+/A/B/C/D.
/// </summary>
public static class DivergenceEngine
{
    private static readonly (string name, Func<Series, double[]> fn, double loZone, double hiZone)[] Oscillators =
    {
        ("rsi",       s => Oscillators.Rsi(s.Close, 14),              30, 70),
        ("stoch_k",   s => Oscillators.Stochastic(s.High,s.Low,s.Close).k, 20, 80),
        ("stoch_rsi", s => Oscillators.StochRsi(s.Close, 14),         20, 80),
        ("cci",       s => Oscillators.Cci(s.High,s.Low,s.Close, 20),-100,100),
        ("williams",  s => Oscillators.WilliamsR(s.High,s.Low,s.Close,14),-80,-20),
        ("cmo",       s => Oscillators.Cmo(s.Close, 14),             -50, 50),
        ("roc",       s => Oscillators.Roc(s.Close, 12),              -5,  5),
        ("tsi",       s => Oscillators.Tsi(s.Close),                 -25, 25),
        ("awesome",   s => Oscillators.Awesome(s.High, s.Low),         0,  0),
        ("trix",      s => Oscillators.Trix(s.Close, 15),              0,  0),
        ("fisher",    s => Oscillators.Fisher(s.High, s.Low, 10),     -2,  2),
        ("ppo",       s => Oscillators.Ppo(s.Close),                  -1,  1),
        ("macd_hist", s => { var m = Oscillators.Macd(s.Close); return m.hist; }, 0, 0),
        ("vortex",    s => Oscillators.Vortex(s.High,s.Low,s.Close,14),0,  0),
    };

    private static List<int> PivotHighs(double[] a, int strength)
    {
        var res = new List<int>();
        for (int i = strength; i < a.Length - strength; i++)
        {
            bool ok = true;
            for (int k = 1; k <= strength; k++)
                if (a[i - k] >= a[i] || a[i + k] >= a[i]) { ok = false; break; }
            if (ok) res.Add(i);
        }
        return res;
    }

    private static List<int> PivotLows(double[] a, int strength)
    {
        var res = new List<int>();
        for (int i = strength; i < a.Length - strength; i++)
        {
            bool ok = true;
            for (int k = 1; k <= strength; k++)
                if (a[i - k] <= a[i] || a[i + k] <= a[i]) { ok = false; break; }
            if (ok) res.Add(i);
        }
        return res;
    }

    public static List<DivergenceEvent> Detect(Series s, int pivotStrength = 3, int maxLookback = 60)
    {
        var results = new List<DivergenceEvent>();
        if (s.Count < 30) return results;
        int end = s.Count - 1;

        foreach (var (name, fn, lo, hi) in Oscillators)
        {
            double[] osc;
            try { osc = fn(s); } catch { continue; }
            if (osc.Length != s.Close.Length) continue;

            var priceHighs = PivotHighs(s.High, pivotStrength);
            var priceLows = PivotLows(s.Low, pivotStrength);
            var oscHighs = PivotHighs(osc, pivotStrength);
            var oscLows = PivotLows(osc, pivotStrength);

            // regular bearish: HH price, LH oscillator
            AddPairwise(results, s, osc, name, priceHighs, oscHighs, "regular_bearish",
                (p1, p2) => s.High[p2] > s.High[p1],
                (o1, o2) => osc[o2] < osc[o1],
                lo, hi, end, maxLookback);
            // regular bullish: LL price, HL oscillator
            AddPairwise(results, s, osc, name, priceLows, oscLows, "regular_bullish",
                (p1, p2) => s.Low[p2] < s.Low[p1],
                (o1, o2) => osc[o2] > osc[o1],
                lo, hi, end, maxLookback);
            // hidden bearish: LH price, HH oscillator (trend continuation)
            AddPairwise(results, s, osc, name, priceHighs, oscHighs, "hidden_bearish",
                (p1, p2) => s.High[p2] < s.High[p1],
                (o1, o2) => osc[o2] > osc[o1],
                lo, hi, end, maxLookback);
            // hidden bullish: HL price, LL oscillator
            AddPairwise(results, s, osc, name, priceLows, oscLows, "hidden_bullish",
                (p1, p2) => s.Low[p2] > s.Low[p1],
                (o1, o2) => osc[o2] < osc[o1],
                lo, hi, end, maxLookback);
        }
        return results;
    }

    private static void AddPairwise(List<DivergenceEvent> outList, Series s, double[] osc, string indicator,
        List<int> pricePivots, List<int> oscPivots, string type,
        Func<int, int, bool> priceCond, Func<int, int, bool> oscCond,
        double loZone, double hiZone, int endIndex, int maxLookback)
    {
        if (pricePivots.Count < 2 || oscPivots.Count < 2) return;
        var p2 = pricePivots[^1]; var p1 = pricePivots[^2];
        var o2 = oscPivots[^1]; var o1 = oscPivots[^2];
        if (endIndex - p2 > maxLookback) return;
        if (Math.Abs(p2 - o2) > 4 || Math.Abs(p1 - o1) > 4) return;
        if (!priceCond(p1, p2) || !oscCond(o1, o2)) return;

        double angleDelta;
        if (type.Contains("bull"))
            angleDelta = Math.Abs((s.Low[p2] - s.Low[p1]) / Math.Max(1, p2 - p1) - (osc[o2] - osc[o1]) / Math.Max(1, o2 - o1));
        else
            angleDelta = Math.Abs((s.High[p2] - s.High[p1]) / Math.Max(1, p2 - p1) - (osc[o2] - osc[o1]) / Math.Max(1, o2 - o1));

        int barsAlive = endIndex - p2;
        double pivotSpan = p2 - p1;
        double freshness = Math.Exp(-barsAlive / 10.0);

        double zoneBonus = 0;
        if (hiZone != loZone)
        {
            if (type.Contains("bull") && osc[o2] < loZone) zoneBonus = 20;
            else if (type.Contains("bear") && osc[o2] > hiZone) zoneBonus = 20;
        }

        double score = 30 * Math.Min(1, angleDelta * 100)
                     + zoneBonus
                     + 15 * freshness
                     + 10 * Math.Min(1, pivotSpan / 30.0)
                     + 25;
        score = Math.Max(0, Math.Min(100, score));
        string grade = score >= 85 ? "A+" : score >= 75 ? "A" : score >= 60 ? "B" : score >= 45 ? "C" : "D";

        outList.Add(new DivergenceEvent
        {
            Type = type,
            Indicator = indicator,
            TimeframeSec = s.TimeframeSec,
            Grade = grade,
            Score = Math.Round(score, 2),
            BarsAlive = barsAlive,
            PivotSpan = pivotSpan,
            AngleDelta = Math.Round(angleDelta, 6)
        });
    }
}
