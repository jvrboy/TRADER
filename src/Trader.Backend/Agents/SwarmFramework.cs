using Trader.Backend.Core;

namespace Trader.Backend.Agents;

/// <summary>Direction a swarm agent votes.</summary>
public enum VoteDirection { Buy, Sell, Neutral }

/// <summary>A single agent's vote on the market.</summary>
public sealed record SwarmVote(
    string Agent,
    string Family,
    VoteDirection Direction,
    double Confidence,   // 0..1
    double Weight);      // agent credibility weight

/// <summary>Aggregated swarm consensus across all specialist agents.</summary>
public sealed record SwarmConsensus(
    VoteDirection Direction,
    double BullScore,
    double BearScore,
    double NetScore,        // -1..1
    double Confidence,      // 0..1
    int AgentsFired,
    IReadOnlyList<SwarmVote> Votes)
{
    /// <summary>0-100 composite signal strength.</summary>
    public double SignalStrength => Math.Round(Math.Abs(NetScore) * 100.0, 1);
}

/// <summary>
/// A specialist agent that examines the market and casts a directional vote.
/// Each agent is self-describing so the coordinator can report which agents
/// fired and how they voted.
/// </summary>
public interface ISwarmAgent
{
    string Name { get; }
    string Family { get; }
    double Weight { get; }
    SwarmVote Evaluate(IReadOnlyList<CandleData> market);
}

/// <summary>
/// Coordinates a swarm of specialist agents: runs every agent, aggregates
/// their weighted votes into a consensus, and reports per-agent detail.
/// </summary>
public sealed class SwarmCoordinator
{
    private readonly IReadOnlyList<ISwarmAgent> _agents;

    public SwarmCoordinator(IEnumerable<ISwarmAgent> agents)
    {
        _agents = agents.ToArray();
    }

    public IReadOnlyList<ISwarmAgent> Agents => _agents;

    public SwarmConsensus Evaluate(IReadOnlyList<CandleData> market)
    {
        var votes = _agents
            .Select(a => a.Evaluate(market))
            .Where(v => v.Direction != VoteDirection.Neutral)
            .ToList();

        double bull = 0, bear = 0;
        foreach (var v in votes)
        {
            var scored = v.Confidence * v.Weight;
            if (v.Direction == VoteDirection.Buy) bull += scored;
            else if (v.Direction == VoteDirection.Sell) bear += scored;
        }

        var total = bull + bear;
        var net = total == 0 ? 0 : (bull - bear) / total;
        var direction = net > 0.1 ? VoteDirection.Buy : net < -0.1 ? VoteDirection.Sell : VoteDirection.Neutral;

        // Confidence: how many agents fired and how strongly they agree
        var agreement = total == 0 ? 0 : (bull > bear ? bull : bear) / total;
        var coverage = _agents.Count == 0 ? 0 : (double)votes.Count / _agents.Count;
        var confidence = agreement * (0.5 + 0.5 * coverage);

        return new SwarmConsensus(direction, bull, bear, net, confidence, votes.Count, votes);
    }
}
