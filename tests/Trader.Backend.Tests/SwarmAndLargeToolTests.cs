using Trader.Backend.Agents;
using Trader.Backend.Core;
using Trader.Backend.Tools;
using Xunit;

namespace Trader.Backend.Tests;

public class SwarmAndLargeToolTests
{
    private static ToolContext Context(int candles = 250, params string[] symbols)
    {
        if (symbols.Length == 0) symbols = new[] { "EURUSD" };
        var market = symbols.SelectMany((s, i) => MarketDataFactory.GenerateSeries(
            s, candles, start: 1.0 + i * 0.5, drift: i % 2 == 0 ? 0.0008 : -0.0004)).ToArray();
        return new ToolContext { Now = DateTimeOffset.UtcNow, Market = market };
    }

    // ---- Swarm framework ----

    [Fact]
    public void Swarm_DefaultHasFiveAgents()
    {
        var swarm = SwarmFactory.Default();
        Assert.Equal(5, swarm.Agents.Count);
    }

    [Fact]
    public void Swarm_ProducesConsensusOnMarket()
    {
        var swarm = SwarmFactory.Default();
        var market = MarketDataFactory.GenerateSeries("EURUSD", 250, start: 1.0850);
        var consensus = swarm.Evaluate(market);

        Assert.InRange(consensus.NetScore, -1, 1);
        Assert.True(consensus.AgentsFired >= 0);
        Assert.True(consensus.AgentsFired <= 5);
        Assert.InRange(consensus.Confidence, 0, 1);
    }

    [Fact]
    public void Swarm_StrongUpTrend_TendsBullish()
    {
        // Strongly upward-drifting series -> swarm should lean buy
        var swarm = SwarmFactory.Default();
        var market = MarketDataFactory.GenerateSeries("EURUSD", 300, start: 1.0, drift: 0.003, vol: 0.005);
        var consensus = swarm.Evaluate(market);

        Assert.True(consensus.BullScore >= consensus.BearScore);
    }

    [Fact]
    public void SwarmAnalyzeTool_ReturnsVotes()
    {
        var tool = new SwarmAnalyzeTool();
        var result = tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD"
        }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.Contains((string)result.Data!["direction"], new[] { "Buy", "Sell", "Neutral" });
        Assert.True((int)result.Data["agentsFired"] <= 5);
    }

    // ---- Large analysis tools ----

    [Fact]
    public void MarketProfile_PocAndValueAreaWithinRange()
    {
        var tool = new MarketProfileTool();
        var result = tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD"
        }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.True((double)result.Data!["valueAreaLow"] <= (double)result.Data["valueAreaHigh"]);
        Assert.True((double)result.Data["poc"] >= (double)result.Data["valueAreaLow"]);
        Assert.True((double)result.Data["poc"] <= (double)result.Data["valueAreaHigh"]);
    }

    [Fact]
    public void Drawdown_MaxDdIsNonNegative()
    {
        var tool = new DrawdownTool();
        var result = tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD"
        }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.True((double)result.Data!["maxDrawdownPct"] >= 0);
    }

    [Fact]
    public void RiskMetrics_ProducesSharpeAndSortino()
    {
        var tool = new SharpeSortinoTool();
        var result = tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD"
        }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.True(result.Data!.ContainsKey("sharpe"));
        Assert.True(result.Data.ContainsKey("sortino"));
    }

    [Fact]
    public void VolSurface_BuildsTenorsAndStrikes()
    {
        var tool = new VolSurfaceTool();
        var result = tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD", ["atmVol"] = "12", ["spot"] = "1.09"
        }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        var surface = (List<Dictionary<string, object>>)result.Data!["surface"];
        Assert.Equal(4, surface.Count); // 4 tenors
    }

    [Fact]
    public void SectorRotation_RanksSymbols()
    {
        var tool = new SectorRotationTool();
        var ctx = Context(250, "TECH", "ENERGY", "FINANCE", "HEALTH");
        var result = tool.ExecuteAsync(ctx, new Dictionary<string, string>()).GetAwaiter().GetResult();

        Assert.True(result.Success);
        var ranked = (List<Dictionary<string, object>>)result.Data!["ranked"];
        Assert.True(ranked.Count >= 2);
    }

    [Fact]
    public void OrderFlow_ProducesPressureLabel()
    {
        var tool = new OrderFlowTool();
        var result = tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD"
        }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.Contains((string)result.Data!["label"], new[] { "strong-buying", "buying", "balanced", "selling", "strong-selling" });
    }

    [Fact]
    public void StrategyBuilder_EvaluatesRule()
    {
        var tool = new StrategyBuilderTool();
        var result = tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD",
            ["rule"] = "rsi_gt_50 AND price_gt_ema9"
        }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.True(result.Data!.ContainsKey("fires"));
    }

    [Fact]
    public void StrategyBuilder_UnknownCondition_Fails()
    {
        var tool = new StrategyBuilderTool();
        var result = tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD",
            ["rule"] = "bogus_condition"
        }).GetAwaiter().GetResult();

        Assert.False(result.Success);
    }

    [Fact]
    public void AllNewTools_Registered()
    {
        var registry = new ToolRegistry();
        registry.Register(new SwarmAnalyzeTool());
        registry.Register(new MarketProfileTool());
        registry.Register(new DrawdownTool());
        registry.Register(new SharpeSortinoTool());
        registry.Register(new VolSurfaceTool());
        registry.Register(new SectorRotationTool());
        registry.Register(new OrderFlowTool());
        registry.Register(new StrategyBuilderTool());

        Assert.Equal(8, registry.All.Count);
        Assert.Contains("swarm.analyze", registry.Names);
        Assert.Contains("strategy.evaluate", registry.Names);
    }
}
