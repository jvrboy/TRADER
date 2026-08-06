using Trader.Backend.Agents;
using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Runs the full swarm of specialist agents over the market and returns the
/// consensus signal with per-agent detail. This is the primary agentic entry
/// point for the backend.
/// </summary>
public sealed class SwarmAnalyzeTool : ITool
{
    private readonly SwarmCoordinator _swarm;

    public SwarmAnalyzeTool()
    {
        _swarm = SwarmFactory.Default();
    }

    public SwarmAnalyzeTool(SwarmCoordinator swarm)
    {
        _swarm = swarm;
    }

    public string Name => "swarm.analyze";
    public string Description => "Run the specialist-agent swarm and return consensus.";
    public string Parameters => "symbol";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? context.Market.FirstOrDefault()?.Symbol ?? "?";
        var market = context.Market.Where(c => c.Symbol == symbol).ToArray();
        if (market.Length < 30)
            return Task.FromResult(ToolResult.Fail($"Not enough data for '{symbol}' to run the swarm."));

        var consensus = _swarm.Evaluate(market);

        var votes = consensus.Votes
            .Select(v => new Dictionary<string, object>
            {
                ["agent"] = v.Agent,
                ["family"] = v.Family,
                ["direction"] = v.Direction.ToString(),
                ["confidence"] = Math.Round(v.Confidence, 2),
            })
            .ToList();

        var data = new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["direction"] = consensus.Direction.ToString(),
            ["bullScore"] = Math.Round(consensus.BullScore, 2),
            ["bearScore"] = Math.Round(consensus.BearScore, 2),
            ["netScore"] = Math.Round(consensus.NetScore, 3),
            ["confidence"] = Math.Round(consensus.Confidence, 3),
            ["signalStrength"] = consensus.SignalStrength,
            ["agentsFired"] = consensus.AgentsFired,
            ["votes"] = votes,
        };

        var message = consensus.Direction == VoteDirection.Neutral
            ? $"{symbol}: swarm undecided ({consensus.AgentsFired} agents fired, strength {consensus.SignalStrength})."
            : $"{symbol}: swarm {consensus.Direction.ToString().ToLowerInvariant()} ({consensus.AgentsFired} agents, strength {consensus.SignalStrength}, conf {consensus.Confidence:0.00}).";

        return Task.FromResult(ToolResult.Ok(message, data));
    }
}
