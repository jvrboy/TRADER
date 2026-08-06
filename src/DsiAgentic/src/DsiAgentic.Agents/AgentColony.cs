using DsiAgentic.Core;
using DsiAgentic.Divergence;
using DsiAgentic.Indicators;
using DsiAgentic.Strategies;

namespace DsiAgentic.Agents;

public sealed class TrendAgent : IAgent
{
    public string Name => "TrendAgent";
    public string Family => "trend";
    public double Weight => 1.0;
    public IEnumerable<Vote> Evaluate(Dictionary<int, Series> mtf)
    {
        var reg = new StrategyRegistry()
            .Register(new TrendRibbonStrategy())
            .Register(new AdxTrendStrategy())
            .Register(new SuperTrendStrategy())
            .Register(new IchimokuStrategy());
        foreach (var kv in mtf)
            foreach (var v in reg.RunAll(kv.Value))
            { v.Agent = $"{Name}@{kv.Key}"; yield return v; }
    }
}

public sealed class MomentumAgent : IAgent
{
    public string Name => "MomentumAgent";
    public string Family => "momentum";
    public double Weight => 1.0;
    public IEnumerable<Vote> Evaluate(Dictionary<int, Series> mtf)
    {
        var reg = new StrategyRegistry()
            .Register(new RsiExtremeStrategy())
            .Register(new MacdCrossStrategy())
            .Register(new StochCrossStrategy())
            .Register(new CciTrendStrategy());
        foreach (var kv in mtf)
            foreach (var v in reg.RunAll(kv.Value))
            { v.Agent = $"{Name}@{kv.Key}"; yield return v; }
    }
}

public sealed class VolatilityAgent : IAgent
{
    public string Name => "VolatilityAgent";
    public string Family => "volatility";
    public double Weight => 0.75;
    public IEnumerable<Vote> Evaluate(Dictionary<int, Series> mtf)
    {
        var reg = new StrategyRegistry()
            .Register(new BollingerReversionStrategy())
            .Register(new KeltnerSqueezeStrategy());
        foreach (var kv in mtf)
            foreach (var v in reg.RunAll(kv.Value))
            { v.Agent = $"{Name}@{kv.Key}"; yield return v; }
    }
}

public sealed class StructureAgent : IAgent
{
    public string Name => "StructureAgent";
    public string Family => "structure";
    public double Weight => 1.1;
    public IEnumerable<Vote> Evaluate(Dictionary<int, Series> mtf)
    {
        var reg = new StrategyRegistry()
            .Register(new DonchianBreakoutStrategy());
        foreach (var kv in mtf)
            foreach (var v in reg.RunAll(kv.Value))
            { v.Agent = $"{Name}@{kv.Key}"; yield return v; }
    }
}

public sealed class SmcAgent : IAgent
{
    public string Name => "SmcAgent";
    public string Family => "smc";
    public double Weight => 1.2;
    public IEnumerable<Vote> Evaluate(Dictionary<int, Series> mtf)
    {
        var reg = new StrategyRegistry()
            .Register(new SmcOrderBlockStrategy())
            .Register(new FvgFillStrategy())
            .Register(new SweepReversalStrategy());
        foreach (var kv in mtf)
            foreach (var v in reg.RunAll(kv.Value))
            { v.Agent = $"{Name}@{kv.Key}"; yield return v; }
    }
}

public sealed class DivergenceAgent : IAgent
{
    public string Name => "DivergenceAgent";
    public string Family => "divergence";
    public double Weight => 1.5;
    public IEnumerable<Vote> Evaluate(Dictionary<int, Series> mtf)
    {
        foreach (var kv in mtf)
        {
            var events = DivergenceEngine.Detect(kv.Value);
            foreach (var e in events)
            {
                double w = e.Grade switch { "A+" => 1.5, "A" => 1.25, "B" => 1.0, "C" => 0.6, _ => 0.3 };
                var dir = e.Type.Contains("bull") ? Direction.Buy : Direction.Sell;
                yield return new Vote
                {
                    Agent = $"{Name}:{e.Indicator}@{kv.Key}",
                    Family = Family,
                    Direction = dir,
                    Weight = w,
                    Confidence = Math.Min(1, e.Score / 100),
                    Reason = $"{e.Type} {e.Grade} {e.Score:F1}"
                };
            }
        }
    }
}

public sealed class RegimeAgent : IAgent
{
    public string Name => "RegimeAgent";
    public string Family => "regime";
    public double Weight => 1.0;
    public IEnumerable<Vote> Evaluate(Dictionary<int, Series> mtf)
    {
        foreach (var kv in mtf)
        {
            var s = kv.Value;
            if (s.Count < 60) continue;
            var (adx, pdi, mdi) = Trend.Adx(s.High, s.Low, s.Close, 14);
            var chop = Trend.Choppiness(s.High, s.Low, s.Close, 14);
            var ema20 = SeriesMath.Ema(s.Close, 20);
            var ema50 = SeriesMath.Ema(s.Close, 50);
            bool trending = adx[^1] > 22 && chop[^1] < 55;
            var dir = ema20[^1] > ema50[^1] ? Direction.Buy : Direction.Sell;
            if (!trending) continue;
            yield return new Vote
            {
                Agent = $"{Name}@{kv.Key}",
                Family = Family,
                Direction = dir,
                Weight = 1.0,
                Confidence = Math.Min(1, adx[^1] / 50),
                Reason = $"adx={adx[^1]:F1} chop={chop[^1]:F1}"
            };
        }
    }
}

public sealed class StatisticalAgent : IAgent
{
    public string Name => "StatisticalAgent";
    public string Family => "statistical";
    public double Weight => 0.85;
    public IEnumerable<Vote> Evaluate(Dictionary<int, Series> mtf)
    {
        foreach (var kv in mtf)
        {
            var s = kv.Value;
            if (s.Count < 60) continue;
            var z = Statistical.ZScore(s.Close);
            var h = Statistical.Hurst(s.Close);
            if (Math.Abs(z) < 1.2) continue;
            var dir = z < 0 ? Direction.Buy : Direction.Sell;
            // if trending market (h>0.55), reverse into momentum
            if (h > 0.55) dir = z < 0 ? Direction.Sell : Direction.Buy;
            yield return new Vote
            {
                Agent = $"{Name}@{kv.Key}",
                Family = Family,
                Direction = dir,
                Weight = 0.85,
                Confidence = Math.Min(1, Math.Abs(z) / 3),
                Reason = $"z={z:F2} hurst={h:F2}"
            };
        }
    }
}

public sealed class CandleAgent : IAgent
{
    public string Name => "CandleAgent";
    public string Family => "candle";
    public double Weight => 0.6;
    public IEnumerable<Vote> Evaluate(Dictionary<int, Series> mtf)
    {
        foreach (var kv in mtf)
        {
            var s = kv.Value; if (s.Count < 3) continue;
            var o = s.Open[^1]; var c = s.Close[^1];
            var op = s.Open[^2]; var cp = s.Close[^2];
            bool bullEng = cp < op && c > o && c > op && o < cp;
            bool bearEng = cp > op && c < o && c < op && o > cp;
            if (bullEng) yield return new Vote { Agent = $"{Name}@{kv.Key}", Family = Family, Direction = Direction.Buy, Weight = 0.6, Confidence = 0.65, Reason = "bull_engulfing" };
            if (bearEng) yield return new Vote { Agent = $"{Name}@{kv.Key}", Family = Family, Direction = Direction.Sell, Weight = 0.6, Confidence = 0.65, Reason = "bear_engulfing" };
        }
    }
}

/// <summary>Confluence orchestrator that aggregates every agent's votes.</summary>
public sealed class AgentColony
{
    public List<IAgent> Agents { get; } = new()
    {
        new TrendAgent(), new MomentumAgent(), new VolatilityAgent(),
        new StructureAgent(), new SmcAgent(), new DivergenceAgent(),
        new RegimeAgent(), new StatisticalAgent(), new CandleAgent()
    };

    public sealed record Confluence(
        Direction Direction, double NetScore, double BullScore, double BearScore,
        int AgentsFired, List<Vote> Votes, Dictionary<string, (double bull, double bear)> Families);

    public Confluence Aggregate(Dictionary<int, Series> mtf)
    {
        var votes = new List<Vote>();
        foreach (var a in Agents)
        {
            foreach (var v in a.Evaluate(mtf))
            {
                v.Weight *= a.Weight;
                votes.Add(v);
            }
        }
        double bull = 0, bear = 0;
        var families = new Dictionary<string, (double bull, double bear)>();
        foreach (var v in votes)
        {
            var scored = v.Weight * v.Confidence;
            if (!families.ContainsKey(v.Family)) families[v.Family] = (0, 0);
            var (b, s) = families[v.Family];
            if (v.Direction == Direction.Buy) { bull += scored; b += scored; }
            else if (v.Direction == Direction.Sell) { bear += scored; s += scored; }
            families[v.Family] = (b, s);
        }
        var net = bull - bear;
        var dir = net > 0 ? Direction.Buy : net < 0 ? Direction.Sell : Direction.Neutral;
        return new Confluence(dir, net, bull, bear, votes.Count, votes, families);
    }
}
