namespace Trader.Backend.Agents;

/// <summary>
/// Builds ready-made swarms. Provides a default swarm with all specialist
/// agents and a method to compose a custom swarm.
/// </summary>
public static class SwarmFactory
{
    /// <summary>The default analysis swarm: trend, momentum, volatility, breakout, volume.</summary>
    public static SwarmCoordinator Default() =>
        new(new ISwarmAgent[]
        {
            new TrendSwarmAgent(),
            new MomentumSwarmAgent(),
            new VolatilitySwarmAgent(),
            new BreakoutSwarmAgent(),
            new VolumeSwarmAgent(),
        });

    /// <summary>Build a custom swarm from a set of specialist agents.</summary>
    public static SwarmCoordinator Custom(params ISwarmAgent[] agents) => new(agents);
}
