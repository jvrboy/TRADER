using DsiAgentic.Core;
using DsiAgentic.Divergence;

namespace DsiAgentic.Strategies;

public sealed class DivergenceStrategy : IStrategy
{
    public string Name => "divergence";
    public string Family => "divergence";
    public double Weight => 1.5;

    public Vote? Evaluate(Series s)
    {
        var events = DivergenceEngine.Detect(s);
        if (events.Count == 0) return null;
        double bull = 0, bear = 0;
        foreach (var e in events)
        {
            double w = e.Grade switch { "A+" => 1.5, "A" => 1.25, "B" => 1.0, "C" => 0.6, _ => 0.3 };
            if (e.Type.Contains("bull")) bull += w; else bear += w;
        }
        var dir = bull > bear ? Direction.Buy : bear > bull ? Direction.Sell : Direction.Neutral;
        if (dir == Direction.Neutral) return null;
        var top = events.OrderByDescending(x => x.Score).First();
        return new Vote { Agent = Name, Family = Family, Direction = dir, Weight = Weight, Confidence = Math.Min(1, top.Score / 100), Reason = $"top={top.Indicator}@{top.TimeframeSec} {top.Type} {top.Grade} {top.Score:F1}" };
    }
}
