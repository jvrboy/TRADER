using Trader.Backend.Core;
using Trader.Backend.Tools;
using Xunit;

namespace Trader.Backend.Tests;

public class ToolFrameworkTests
{
    [Fact]
    public void Registry_RegistersAndDiscoversTools()
    {
        var registry = new ToolRegistry();
        registry.Register(new TechnicalScannerTool());
        registry.Register(new RiskEngineTool());

        Assert.Equal(2, registry.All.Count);
        Assert.Contains("tech.scan", registry.Names);
        Assert.True(registry.TryGet("risk.assess", out var tool));
        Assert.NotNull(tool);
    }

    [Fact]
    public void Agent_RunsSingleTool()
    {
        var registry = new ToolRegistry();
        registry.Register(new MarketRegimeTool());
        var agent = new Agent("test", registry);

        var market = MarketDataFactory.GenerateSeries("EURUSD", 100);
        var context = new ToolContext { Now = DateTimeOffset.UtcNow, Market = market };

        var result = agent.RunToolAsync(context, "market.regime",
            new Dictionary<string, string> { ["symbol"] = "EURUSD" }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("EURUSD", result.Data!["symbol"]);
    }

    [Fact]
    public void Agent_UnknownTool_ReturnsFailure()
    {
        var registry = new ToolRegistry();
        var agent = new Agent("test", registry);
        var context = new ToolContext { Now = DateTimeOffset.UtcNow, Market = Array.Empty<CandleData>() };

        var result = agent.RunToolAsync(context, "does.not.exist",
            new Dictionary<string, string>()).GetAwaiter().GetResult();

        Assert.False(result.Success);
        Assert.Contains("Unknown tool", result.Message);
    }

    [Fact]
    public void Agent_RunsPlan_InOrder()
    {
        var registry = new ToolRegistry();
        registry.Register(new TechnicalScannerTool());
        registry.Register(new MarketRegimeTool());
        var agent = new Agent("test", registry);

        var market = MarketDataFactory.GenerateSeries("EURUSD", 120);
        var context = new ToolContext { Now = DateTimeOffset.UtcNow, Market = market };

        var plan = new[]
        {
            new AgentStep("market.regime", new Dictionary<string, string> { ["symbol"] = "EURUSD" }),
            new AgentStep("tech.scan", new Dictionary<string, string> { ["symbol"] = "EURUSD" }),
        };

        var results = agent.RunPlanAsync(context, plan).GetAwaiter().GetResult();

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
    }
}
