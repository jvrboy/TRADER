using AetherBrain.Agents;

namespace AetherBrain.Learning;

public sealed class AdaptiveWeightEngine
{
    private readonly Dictionary<string, double> _weights = new(StringComparer.OrdinalIgnoreCase);

    public double Combine(IReadOnlyList<AgentResult> results)
    {
        if (results.Count == 0) return 0;
        var weighted = results.Sum(result => result.Confidence * Weight(result.Agent));
        var total = results.Sum(result => Weight(result.Agent));
        return total == 0 ? 0 : weighted / total;
    }

    public void Reinforce(IEnumerable<AgentResult> results, double reward)
    {
        foreach (var result in results)
            _weights[result.Agent] = Math.Clamp(Weight(result.Agent) + reward * (result.Confidence - .5), .25, 2.5);
    }

    private double Weight(string agent) => _weights.TryGetValue(agent, out var value) ? value : 1;
}
