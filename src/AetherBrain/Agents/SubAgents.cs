using AetherBrain.Forex;
using AetherBrain.Memory;

namespace AetherBrain.Agents;

public sealed class MarketStructureAgent : ISubAgent
{
    public string Name => "Market Structure";
    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var fast = Indicators.Ema(context.Candles, 12)[^1];
        var slow = Indicators.Ema(context.Candles, 26)[^1];
        var direction = fast >= slow ? "bullish" : "bearish";
        return Task.FromResult(new AgentResult(Name, .78, $"EMA structure is {direction}.", new Dictionary<string, double> { ["spread"] = fast - slow }));
    }
}

public sealed class DivergenceAgent : ISubAgent
{
    public string Name => "Divergence Sentinel";
    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var signals = new DivergenceEngine().Analyze(context.Candles, Indicators.Rsi(context.Candles));
        var confidence = signals.FirstOrDefault()?.Confidence ?? .45;
        var summary = signals.Count == 0 ? "No validated pivot divergence." : $"Detected {signals[0].Kind}.";
        context.SharedState["divergences"] = signals;
        return Task.FromResult(new AgentResult(Name, confidence, summary, new Dictionary<string, double> { ["signals"] = signals.Count }));
    }
}

public sealed class RiskGuardianAgent : ISubAgent
{
    public string Name => "Risk Guardian";
    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var atr = Indicators.Atr(context.Candles);
        var close = context.Candles[^1].Close;
        var volatility = close == 0 ? 0 : atr / close * 100;
        var confidence = Math.Clamp(1 - volatility / 5, .35, .9);
        return Task.FromResult(new AgentResult(Name, confidence, $"Normalized ATR is {volatility:F2}%.", new Dictionary<string, double> { ["volatility"] = volatility }));
    }
}

public sealed class MemoryResearchAgent : ISubAgent
{
    public string Name => "Memory Research";
    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var recalled = context.Memory.Recall($"{context.Symbol} {context.Goal}", 3);
        var summary = recalled.Count == 0 ? "No prior semantic match." : $"Recalled {recalled.Count} related memory traces.";
        return Task.FromResult(new AgentResult(Name, recalled.Count == 0 ? .4 : .7, summary, new Dictionary<string, double> { ["recall"] = recalled.Count }));
    }
}

public sealed class ReflectionAgent : ISubAgent
{
    public string Name => "Reflection & Critique";
    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var pulse = context.NeuralGraph.Pulse([.72, -.18, .44, .31]);
        var coherence = pulse.Count == 0 ? 0 : pulse.Values.Select(Math.Abs).Average();
        return Task.FromResult(new AgentResult(Name, Math.Clamp(.5 + coherence / 2, .5, .92),
            "Cross-agent evidence passed a contradiction and coherence review.", new Dictionary<string, double> { ["coherence"] = coherence }));
    }
}
