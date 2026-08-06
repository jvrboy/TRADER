using AetherBrain.Core;
using AetherBrain.Forex;
using AetherBrain.Memory;

namespace AetherBrain.Agents;

public sealed record AgentContext(
    string Goal,
    string Symbol,
    IReadOnlyList<Candle> Candles,
    CognitiveMemory Memory,
    NeuralGraph NeuralGraph,
    Dictionary<string, object> SharedState);

public sealed record AgentResult(string Agent, double Confidence, string Summary, IReadOnlyDictionary<string, double> Metrics);

public interface ISubAgent
{
    string Name { get; }
    Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default);
}
