using NexusBrain.Core;
using NexusBrain.Indicators;

namespace NexusBrain.Agents;

/// <summary>Risk agent: computes risk metrics and warns when volatility demands caution.</summary>
public sealed class RiskAgent : SubAgentBase
{
    public override string Name => "risk";
    public override string Description => "Assesses risk: ATR-based sizing, drawdown risk, volatility warning.";
    public override double Weight => 1.3;

    public override IEnumerable<Signal> Analyze(Candle[] candles, string symbol, InstrumentKind kind)
    {
        if (candles.Length < 30) yield break;
        var n = candles.Length;
        var c = new double[n]; var h = new double[n]; var l = new double[n];
        for (int i = 0; i < n; i++) { c[i] = candles[i].Close; h[i] = candles[i].High; l[i] = candles[i].Low; }

        var atr = SeriesMath.Atr(h, l, c, 14);
        var vi = Volatility.VolatilityIndex(c, h, l);
        double atrPct = atr[n - 1] / c[n - 1];
        double v = vi[n - 1];

        if (v > 0.75)
        {
            yield return new Signal
            {
                Symbol = symbol, Bias = Bias.Neutral, Confidence = 0.7,
                Strength = v, Agent = Name,
                Reason = $"HIGH RISK: Volatility Index {v:P0} → reduce size, widen stops",
                Epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Metrics = new Dictionary<string, double> { ["vi"] = v, ["atr_pct"] = atrPct }
            };
        }
        else if (atrPct > 0.03)
        {
            yield return new Signal
            {
                Symbol = symbol, Bias = Bias.Neutral, Confidence = 0.5,
                Strength = atrPct, Agent = Name,
                Reason = $"Elevated ATR {atrPct:P2} → size positions conservatively",
                Epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Metrics = new Dictionary<string, double> { ["atr_pct"] = atrPct }
            };
        }
    }
}

/// <summary>
/// The agent colony: runs all sub-agents in parallel over a market snapshot and
/// aggregates their weighted signals into a consensus bias.
/// </summary>
public sealed class AgentColony
{
    private readonly List<ISubAgent> _agents = new();

    public AgentColony()
    {
        _agents.Add(new MomentumAgent());
        _agents.Add(new MeanReversionAgent());
        _agents.Add(new DivergenceAgent());
        _agents.Add(new VolatilityAgent());
        _agents.Add(new DriftSwitchAgent());
        _agents.Add(new ForexAgent());
        _agents.Add(new TrendAgent());
        _agents.Add(new RiskAgent());
    }

    public IReadOnlyList<ISubAgent> Agents => _agents;

    /// <summary>Run all agents and return their signals.</summary>
    public List<Signal> RunAll(Candle[] candles, string symbol, InstrumentKind kind)
    {
        var results = new List<Signal>();
        foreach (var agent in _agents)
        {
            try
            {
                results.AddRange(agent.Analyze(candles, symbol, kind));
            }
            catch
            {
                // A failing agent must not take down the colony.
            }
        }
        return results;
    }

    /// <summary>Aggregate signals into a consensus bias with confidence.</summary>
    public (Bias Bias, double Confidence, double Score) Aggregate(IEnumerable<Signal> signals)
    {
        var list = signals.ToList();
        if (list.Count == 0) return (Bias.Neutral, 0, 0);
        double bullScore = 0, bearScore = 0, bullW = 0, bearW = 0;
        foreach (var s in list)
        {
            var w = WeightOf(s.Agent) * s.Confidence;
            if (s.Bias == Bias.Bullish) { bullScore += w * s.Strength; bullW += w; }
            else if (s.Bias == Bias.Bearish) { bearScore += w * s.Strength; bearW += w; }
        }
        double net = bullScore - bearScore;
        double totalW = bullW + bearW;
        var bias = net > 0.1 ? Bias.Bullish : net < -0.1 ? Bias.Bearish : Bias.Neutral;
        double conf = totalW == 0 ? 0 : Math.Clamp(Math.Abs(net) / Math.Max(1, totalW), 0, 1);
        return (bias, conf, net);
    }

    private double WeightOf(string agentName)
    {
        var a = _agents.FirstOrDefault(x => x.Name == agentName);
        return a?.Weight ?? 1.0;
    }
}
