using DsiAgentic.Agents;
using DsiAgentic.Brains;
using DsiAgentic.Core;
using DsiAgentic.Indicators;
using DsiAgentic.Learning;
using DsiAgentic.Risk;

namespace DsiAgentic.Signals;

public sealed class SignalEngine
{
    private readonly AgentColony _colony;
    private readonly MetaLearner _meta;
    private readonly Dictionary<string, BrainEnsemble> _brains;
    private readonly RootConfig _cfg;

    public SignalEngine(RootConfig cfg, AgentColony colony, MetaLearner meta, Dictionary<string, BrainEnsemble> brains)
    { _cfg = cfg; _colony = colony; _meta = meta; _brains = brains; }

    public Signal? Generate(InstrumentConfig inst, Dictionary<int, Series> mtf)
    {
        if (!mtf.TryGetValue(inst.Timeframes[0], out var primary) || primary.Count < 60) return null;

        var confluence = _colony.Aggregate(mtf);

        // apply meta-learner reweighting
        double bull = 0, bear = 0;
        foreach (var v in confluence.Votes)
        {
            var mult = _meta.AgentMultiplier(v.Agent) * _meta.FamilyMultiplier(v.Family);
            var scored = v.Weight * v.Confidence * mult;
            if (v.Direction == Direction.Buy) bull += scored;
            else if (v.Direction == Direction.Sell) bear += scored;
        }
        var net = bull - bear;
        var dir = net > 0 ? Direction.Buy : net < 0 ? Direction.Sell : Direction.Neutral;
        if (dir == Direction.Neutral) return null;
        if (Math.Abs(net) < inst.MinConfluence) return null;

        var (tp, sl) = RiskManager.ComputeTpSl(primary, dir, inst.AtrTpMult, inst.AtrSlMult);

        var features = FeatureExtractor.Extract(primary);
        double winProb = 0.5;
        if (_brains.TryGetValue(inst.Name, out var brain))
            winProb = brain.PredictWinProbability(features);
        var mcProb = RiskManager.MonteCarloWinProb(primary, dir, tp, sl);
        winProb = 0.5 * winProb + 0.5 * mcProb;

        var (adx, pdi, mdi) = Trend.Adx(primary.High, primary.Low, primary.Close, 14);
        var regime = pdi[^1] > mdi[^1] && adx[^1] > 20 ? Regime.PositiveDrift
                    : mdi[^1] > pdi[^1] && adx[^1] > 20 ? Regime.NegativeDrift
                    : adx[^1] < 15 ? Regime.RangeBound : Regime.Choppy;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var featuresDict = new Dictionary<string, double>();
        for (int i = 0; i < features.Length; i++) featuresDict[$"f{i:D2}"] = features[i];

        return new Signal
        {
            Id = $"{inst.Name}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}",
            Instrument = inst.Name,
            Symbol = inst.Symbol,
            Direction = dir,
            Entry = primary.Close[^1],
            TakeProfit = tp,
            StopLoss = sl,
            Confluence = Math.Round(Math.Abs(net), 3),
            BullScore = Math.Round(bull, 3),
            BearScore = Math.Round(bear, 3),
            AgentsFired = confluence.AgentsFired,
            Regime = regime,
            RegimeAgePct = 0,
            ExpiryRule = _cfg.Runtime.ExpiryRule,
            Status = SignalStatus.Active,
            WinProbability = Math.Round(winProb, 4),
            CreatedEpoch = now,
            Votes = confluence.Votes,
            Features = featuresDict
        };
    }
}
