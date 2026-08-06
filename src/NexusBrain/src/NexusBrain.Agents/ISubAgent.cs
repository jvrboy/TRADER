using NexusBrain.Core;

namespace NexusBrain.Agents;

/// <summary>Base contract for a sub-agent in the brain's colony.</summary>
public interface ISubAgent
{
    string Name { get; }
    string Description { get; }
    /// <summary>Analyse a candle series and emit one or more signals.</summary>
    IEnumerable<Signal> Analyze(Candle[] candles, string symbol, InstrumentKind kind);
    /// <summary>Confidence weight this agent carries in the aggregate vote.</summary>
    double Weight { get; }
}

/// <summary>Base helper for agents.</summary>
public abstract class SubAgentBase : ISubAgent
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual double Weight => 1.0;
    public abstract IEnumerable<Signal> Analyze(Candle[] candles, string symbol, InstrumentKind kind);
}
