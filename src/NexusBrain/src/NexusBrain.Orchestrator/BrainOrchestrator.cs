using NexusBrain.Agents;
using NexusBrain.Brain;
using NexusBrain.Core;
using NexusBrain.Deriv;
using NexusBrain.Divergence;
using NexusBrain.Indicators;
using NexusBrain.Memory;
using BrainType = NexusBrain.Brain.Brain;
using DivergenceType = NexusBrain.Divergence.Divergence;

namespace NexusBrain.Orchestrator;

/// <summary>Result of a full brain run on one symbol.</summary>
public sealed class BrainRunResult
{
    public required BrainAnalysis Analysis { get; init; }
    public double BrainPrediction { get; init; }
    public int SignalsProduced { get; init; }
    public double TrainingLoss { get; init; }
    public int EpisodicMemoryCount { get; init; }
    public int SemanticMemoryCount { get; init; }
    public List<DivergenceType> Divergences { get; init; } = new();
}

/// <summary>
/// The orchestrator — the "CEO" of the brain. Coordinates the neural brain,
/// the self-learning engine, the agent colony, the memory system and the Deriv
/// data feed into a single self-learning analysis loop.
/// </summary>
public sealed class BrainOrchestrator
{
    private readonly BrainConfig _config;
    private readonly BrainType _brain;
    private readonly MemorySystem _memory;
    private readonly AgentColony _colony;
    private readonly DerivClient _deriv;
    private readonly Random _rng;

    public BrainType Brain => _brain;
    public MemorySystem Memory => _memory;
    public AgentColony Colony => _colony;
    public BrainConfig Config => _config;

    public BrainOrchestrator(BrainConfig? config = null)
    {
        _config = config ?? new BrainConfig();
        _config.EnsureDirectories();
        _brain = BrainType.CreateDefault("nexus-primary", Path.Combine(_config.BrainDir, "nexus-primary.json"), _config.HiddenUnits);
        _memory = new MemorySystem(_config.MaxEpisodicMemories, _config.WorkingMemorySlots);
        _colony = new AgentColony();
        _deriv = new DerivClient(_config.DerivWsUrl);
        _rng = new Random();
        _memory.Load(_config.KnowledgeDir);
        _brain.Load();
    }

    /// <summary>Run the full brain on a candle series (offline analysis).</summary>
    public BrainRunResult Analyze(Candle[] candles, string symbol, InstrumentKind kind)
    {
        var features = FeatureExtractor.Extract(candles, symbol);
        double prediction = _brain.Predict(features);

        // Run the agent colony
        var signals = _colony.RunAll(candles, symbol, kind);
        var (aggBias, aggConf, _) = _colony.Aggregate(signals);

        // Divergences for reporting
        var divergences = DivergenceEngine.Scan(candles, symbol);

        // Build the analysis
        var analysis = new BrainAnalysis
        {
            Symbol = symbol,
            Kind = kind,
            AggregateBias = aggBias,
            AggregateConfidence = aggConf,
        };
        analysis.Signals.AddRange(signals);

        // Regime info
        var c = candles.Select(x => x.Close).ToArray();
        var h = candles.Select(x => x.High).ToArray();
        var l = candles.Select(x => x.Low).ToArray();
        var vi = Volatility.VolatilityIndex(c, h, l);
        var dsi = Volatility.DriftSwitchIndex(c, h, l);
        double viV = vi[vi.Length - 1], dsiV = dsi[dsi.Length - 1];
        analysis.Regime["vi"] = viV;
        analysis.Regime["dsi"] = dsiV;
        analysis.Regime["vi_regime"] = viV < 0.35 ? 0 : viV < 0.6 ? 1 : viV < 0.8 ? 2 : 3;
        analysis.Regime["dsi_regime"] = dsiV < 0.4 ? 0 : dsiV < 0.65 ? 1 : 2;
        analysis.Notes["vi"] = Volatility.RegimeLabel(viV);
        analysis.Notes["dsi"] = Volatility.DriftLabel(dsiV);
        analysis.Notes["brain_prediction"] = $"{(prediction >= 0 ? "bullish" : "bearish")} ({prediction:+0.00;-0.00})";

        // Store a working-memory snapshot
        _memory.SetWorking($"last_{symbol}", new
        {
            symbol,
            prediction,
            aggregate = aggBias.ToString(),
            vi = viV,
            dsi = dsiV,
            signals = signals.Count
        });

        return new BrainRunResult
        {
            Analysis = analysis,
            BrainPrediction = prediction,
            SignalsProduced = signals.Count,
            EpisodicMemoryCount = _memory.EpisodicCount,
            SemanticMemoryCount = _memory.SemanticCount,
            Divergences = divergences
        };
    }

    /// <summary>Train the brain on a labelled dataset (offline training).</summary>
    public double Train(IEnumerable<(double[] Features, double Target)> dataset, int epochs = 5)
    {
        var data = dataset.ToList();
        double bestAcc = 0;
        for (int e = 0; e < epochs; e++)
        {
            var acc = _brain.TrainEpoch(data, reinforce: false);
            bestAcc = Math.Max(bestAcc, acc);
            _brain.Replay(32);
        }
        _brain.Save();
        return bestAcc;
    }

    /// <summary>
    /// Self-learning online step: given a snapshot and its realised outcome,
    /// reinforce the brain with the reward signal and record the memory.
    /// </summary>
    public double LearnFromOutcome(Candle[] candles, string symbol, InstrumentKind kind, double realisedReturn)
    {
        var features = FeatureExtractor.Extract(candles, symbol);
        double target = Math.Clamp(realisedReturn / 0.01, -1, 1);
        double reward = realisedReturn > 0 ? 1.0 : -1.0;

        // Reinforce the brain
        double loss = _brain.TrainReinforced(features, target, reward);

        // Record episodic memory
        _memory.Remember(new EpisodicMemory
        {
            Symbol = symbol,
            Event = realisedReturn > 0 ? "PROFIT" : "LOSS",
            Signature = features,
            Outcome = realisedReturn,
            Note = $"kind={kind}, ret={realisedReturn:P2}"
        });

        // Update semantic memory about the symbol's recent behaviour
        _memory.StoreFact($"behaviour_{symbol}", $"last realised return {realisedReturn:P2}", 0.5);

        _brain.Save();
        _memory.Save(_config.KnowledgeDir);
        return loss;
    }

    /// <summary>Fetch live candles from Deriv and run the full brain on them.</summary>
    public async Task<BrainRunResult?> AnalyzeLiveAsync(string symbol, InstrumentKind kind, int granularitySec = 60, int count = 300)
    {
        var candles = await _deriv.GetCandlesAsync(symbol, granularitySec, count);
        if (candles.Count < 40) return null;
        return Analyze(candles.ToArray(), symbol, kind);
    }

    /// <summary>Check Deriv connectivity.</summary>
    public async Task<bool> TestDerivAsync() => await _deriv.PingAsync();

    /// <summary>Persist all brain state.</summary>
    public void SaveAll()
    {
        _brain.Save();
        _memory.Save(_config.KnowledgeDir);
    }

    public async ValueTask DisposeAsync() => await _deriv.DisposeAsync();
}
