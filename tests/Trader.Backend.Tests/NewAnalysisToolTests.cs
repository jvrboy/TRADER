using Trader.Backend.Core;
using Trader.Backend.Tools;
using Xunit;

namespace Trader.Backend.Tests;

public class NewAnalysisToolTests
{
    private static ToolContext Context(int candles = 250, params string[] symbols)
    {
        if (symbols.Length == 0) symbols = new[] { "EURUSD" };
        var market = symbols.SelectMany(s =>
            MarketDataFactory.GenerateSeries(s, candles, start: s == "EURUSD" ? 1.0850 : 1.2700)).ToArray();
        return new ToolContext { Now = DateTimeOffset.UtcNow, Market = market };
    }

    [Fact]
    public void Correlation_IdenticalSeries_IsOne()
    {
        var tool = new CorrelationTool();
        // Two series with the same seed pattern -> highly correlated
        var ctx = Context(120, "EURUSD", "GBPUSD");
        var result = tool.ExecuteAsync(ctx, new Dictionary<string, string>
        {
            ["symbolA"] = "EURUSD", ["symbolB"] = "GBPUSD"
        }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.InRange((double)result.Data!["correlation"], -1, 1);
    }

    [Fact]
    public void Correlation_MissingSymbolB_Fails()
    {
        var tool = new CorrelationTool();
        var result = tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbolA"] = "EURUSD"
        }).GetAwaiter().GetResult();

        Assert.False(result.Success);
        Assert.Contains("symbolB", result.Message);
    }

    [Fact]
    public void Volatility_ReturnsPositiveAtrAndRegime()
    {
        var tool = new VolatilityTool();
        var result = tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD"
        }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.True((double)result.Data!["atr"] > 0);
        Assert.Contains((string)result.Data["regime"], new[] { "high", "elevated", "normal", "low" });
    }

    [Fact]
    public void SupportResistance_LevelsStraddlePrice()
    {
        var tool = new SupportResistanceTool();
        var result = tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD"
        }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        var current = (double)result.Data!["current"];
        Assert.True((double)result.Data["nearestSupport"] <= current);
        Assert.True((double)result.Data["nearestResistance"] >= current);
    }

    [Fact]
    public void Momentum_ScoreIsBoundedAndLabelled()
    {
        var tool = new MomentumTool();
        var result = tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD"
        }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.InRange((double)result.Data!["score"], -100, 100);
        Assert.Contains((string)result.Data["label"], new[] { "strong-bullish", "mild-bullish", "neutral", "mild-bearish", "strong-bearish" });
    }

    [Fact]
    public void RiskReward_GoodSetup_GradesWell()
    {
        var tool = new RiskRewardTool();
        var result = tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["entry"] = "100", ["stopLoss"] = "95", ["takeProfit"] = "115", ["direction"] = "buy"
        }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.Equal(3.0, (double)result.Data!["riskReward"], 1); // 15 reward / 5 risk
        Assert.Equal("excellent", result.Data["grade"]);
    }

    [Fact]
    public void RiskReward_InvalidDirection_Fails()
    {
        var tool = new RiskRewardTool();
        // For a buy, stopLoss must be below entry and takeProfit above.
        var bad = tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["entry"] = "100", ["stopLoss"] = "105", ["takeProfit"] = "95", ["direction"] = "buy"
        }).GetAwaiter().GetResult();

        Assert.False(bad.Success);
    }

    [Fact]
    public void Volume_ComputesRelativeVolume()
    {
        var tool = new VolumeTool();
        var result = tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD"
        }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.True((double)result.Data!["relativeVolume"] > 0);
        Assert.Contains((string)result.Data["signal"], new[] { "confirmed-up", "confirmed-down", "weak-up", "weak-down" });
    }

    [Fact]
    public void AllNewTools_RegisteredInRegistry()
    {
        var registry = new ToolRegistry();
        registry.Register(new CorrelationTool());
        registry.Register(new VolatilityTool());
        registry.Register(new SupportResistanceTool());
        registry.Register(new MomentumTool());
        registry.Register(new RiskRewardTool());
        registry.Register(new VolumeTool());

        Assert.Equal(6, registry.All.Count);
        Assert.Contains("analysis.correlation", registry.Names);
        Assert.Contains("analysis.riskreward", registry.Names);
    }
}
