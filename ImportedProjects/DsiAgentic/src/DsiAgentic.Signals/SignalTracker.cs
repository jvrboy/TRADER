using DsiAgentic.Brains;
using DsiAgentic.Core;
using DsiAgentic.Learning;

namespace DsiAgentic.Signals;

public sealed class SignalTracker
{
    private readonly MetaLearner _meta;
    private readonly Dictionary<string, BrainEnsemble> _brains;
    private readonly KnowledgeStore _knowledge;

    public SignalTracker(MetaLearner meta, Dictionary<string, BrainEnsemble> brains, KnowledgeStore ks)
    { _meta = meta; _brains = brains; _knowledge = ks; }

    /// <summary>Returns true if signal was closed (TP or SL hit). Expiry_rule enforced: TP_OR_SL_ONLY.</summary>
    public bool Update(Signal sig, double quote)
    {
        if (sig.Status != SignalStatus.Active) return false;
        bool tpHit = sig.Direction == Direction.Buy ? quote >= sig.TakeProfit : quote <= sig.TakeProfit;
        bool slHit = sig.Direction == Direction.Buy ? quote <= sig.StopLoss : quote >= sig.StopLoss;
        if (!tpHit && !slHit) return false;
        sig.Status = tpHit ? SignalStatus.TpHit : SignalStatus.SlHit;
        sig.ClosedEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        sig.ClosedQuote = quote;
        double pipMult = sig.Symbol.Contains("JPY") ? 100.0 : 10000.0;
        sig.PnlPips = Math.Round((sig.Direction == Direction.Buy ? quote - sig.Entry : sig.Entry - quote) * pipMult, 1);

        bool won = sig.Status == SignalStatus.TpHit;
        _meta.Record(sig, won);

        // train brain
        if (_brains.TryGetValue(sig.Instrument, out var brain))
        {
            var x = new double[FeatureExtractor.Dim];
            for (int i = 0; i < FeatureExtractor.Dim; i++)
                if (sig.Features.TryGetValue($"f{i:D2}", out var v)) x[i] = v;
            double target = won ? 1.0 : 0.0;
            brain.LearnFromOutcome(x, target, target, 1 - target, target, target);
        }

        _knowledge.Append(sig);
        return true;
    }
}
