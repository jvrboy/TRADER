using DsiAgentic.Agents;
using DsiAgentic.Brains;
using DsiAgentic.Core;
using DsiAgentic.Deriv;
using DsiAgentic.Learning;
using DsiAgentic.Persistence;
using DsiAgentic.Signals;

namespace DsiAgentic.Orchestrator;

/// <summary>
/// Coordinates the live workflow:
///   1) fetch multi-timeframe candles from Deriv
///   2) run the agent colony
///   3) generate signals when confluence exceeds threshold
///   4) poll live quotes, close on TP or SL only
///   5) learn from outcomes and persist state
/// </summary>
public sealed class MasterOrchestrator : IAsyncDisposable
{
    private readonly RootConfig _cfg;
    public DerivClient Client { get; }
    public AgentColony Colony { get; }
    public MetaLearner Meta { get; }
    public KnowledgeStore Knowledge { get; }
    public Dictionary<string, BrainEnsemble> Brains { get; }
    public SignalEngine Engine { get; }
    public SignalTracker Tracker { get; }
    public SignalStore Store { get; }
    public PerformanceStore Performance { get; }
    public MessageBus Bus { get; }

    private readonly string _dataDir;
    private readonly string _brainDir;

    public MasterOrchestrator(RootConfig cfg, string dataDir)
    {
        _cfg = cfg;
        _dataDir = dataDir;
        _brainDir = Path.Combine(dataDir, "brains");
        Directory.CreateDirectory(_dataDir);
        Directory.CreateDirectory(_brainDir);

        Client = new DerivClient(cfg.WsUrl);
        Colony = new AgentColony();
        Meta = MetaLearner.LoadOrNew(Path.Combine(dataDir, "meta_learner.json"));
        Knowledge = new KnowledgeStore(Path.Combine(dataDir, "knowledge"));

        Brains = new Dictionary<string, BrainEnsemble>();
        foreach (var inst in cfg.Instruments)
            Brains[inst.Name] = new BrainEnsemble(inst.Name, FeatureExtractor.Dim);

        Engine = new SignalEngine(cfg, Colony, Meta, Brains);
        Tracker = new SignalTracker(Meta, Brains, Knowledge);
        Store = new SignalStore(dataDir);
        Performance = new PerformanceStore(dataDir);
        Bus = new MessageBus();
    }

    public async Task<Dictionary<int, Series>> LoadMtfAsync(InstrumentConfig inst, CancellationToken ct = default)
    {
        var dict = new Dictionary<int, Series>();
        foreach (var tf in inst.Timeframes)
        {
            var candles = await Client.GetCandlesAsync(inst.Symbol, tf, _cfg.Runtime.CandlesLookback, ct);
            if (candles.Count > 0) dict[tf] = new Series(inst.Symbol, tf, candles);
        }
        return dict;
    }

    public async Task<Signal?> ScanInstrumentAsync(InstrumentConfig inst, CancellationToken ct = default)
    {
        var mtf = await LoadMtfAsync(inst, ct);
        if (mtf.Count == 0) return null;
        var sig = Engine.Generate(inst, mtf);
        if (sig != null) Bus.Publish("signal.new", sig);
        return sig;
    }

    public async Task<List<Signal>> ScanAllAsync(CancellationToken ct = default)
    {
        var open = Store.LoadOpen();
        var results = new List<Signal>();
        foreach (var inst in _cfg.Instruments)
        {
            if (_cfg.Runtime.OnePerInstrument && open.Any(o => o.Instrument == inst.Name && o.Status == SignalStatus.Active))
                continue;
            try
            {
                var s = await ScanInstrumentAsync(inst, ct);
                if (s != null) { results.Add(s); open.Add(s); }
            }
            catch (Exception ex) { Bus.Publish("scan.error", new { instrument = inst.Name, error = ex.Message }); }
        }
        Store.SaveOpen(open);
        return results;
    }

    public async Task<List<Signal>> PollOpenAsync(CancellationToken ct = default)
    {
        var open = Store.LoadOpen();
        var closed = new List<Signal>();
        foreach (var sig in open.Where(o => o.Status == SignalStatus.Active).ToList())
        {
            var quote = await Client.GetQuoteAsync(sig.Symbol, ct);
            if (quote is null) continue;
            if (Tracker.Update(sig, quote.Value))
            {
                Performance.Record(sig);
                Store.AppendHistory(sig);
                closed.Add(sig);
                Bus.Publish("signal.closed", sig);
            }
        }
        var stillOpen = open.Where(o => o.Status == SignalStatus.Active).ToList();
        Store.SaveOpen(stillOpen);
        SaveState();
        return closed;
    }

    public void SaveState()
    {
        Meta.Save(Path.Combine(_dataDir, "meta_learner.json"));
        foreach (var kv in Brains) kv.Value.Save(_brainDir);
        Performance.Save();
    }

    public async ValueTask DisposeAsync()
    {
        SaveState();
        await Client.DisposeAsync();
    }
}
