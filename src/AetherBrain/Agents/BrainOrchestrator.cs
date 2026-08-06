using AetherBrain.Core;
using AetherBrain.Forex;
using AetherBrain.Learning;
using AetherBrain.Memory;

namespace AetherBrain.Agents;

public sealed record BrainReport(string Goal, ForexAnalysis Analysis, IReadOnlyList<AgentResult> Agents, double Consensus, string Decision);

public sealed class BrainOrchestrator
{
    private readonly CognitiveMemory _memory = new();
    private readonly NeuralGraph _graph = new();
    private readonly AdaptiveWeightEngine _learning = new();
    private readonly ForexAnalysisEngine _forex = new();
    private readonly IReadOnlyList<ISubAgent> _agents;

    public BrainOrchestrator()
    {
        for (var index = 0; index < 12; index++) _graph.Add($"cortex-{index + 1}", (index % 3 - 1) * .04);
        _graph.ConnectDense();
        _agents = [new MarketStructureAgent(), new DivergenceAgent(), new RiskGuardianAgent(), new MemoryResearchAgent(), new ReflectionAgent()];
    }

    public async Task<BrainReport> ThinkAsync(string goal, string symbol, IReadOnlyList<Candle> candles, CancellationToken cancellationToken = default)
    {
        var analysis = _forex.Analyze(symbol, candles);
        var context = new AgentContext(goal, symbol, candles, _memory, _graph, []);
        var tasks = _agents.Select(agent => agent.ExecuteAsync(context, cancellationToken));
        var results = await Task.WhenAll(tasks);
        var consensus = _learning.Combine(results);
        var decision = consensus >= .68 && analysis.RiskScore < 62
            ? "Evidence is coherent enough for monitored hypothesis testing."
            : "Evidence remains mixed; preserve capital and gather more data.";
        _memory.Remember($"{symbol}: {analysis.Narrative} Consensus {consensus:P0}. {decision}", MemoryLayer.Episodic, consensus);
        _memory.Consolidate();
        _learning.Reinforce(results, analysis.Divergences.Count > 0 ? .06 : -.02);
        return new BrainReport(goal, analysis, results, consensus, decision);
    }
}
