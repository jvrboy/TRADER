using Trader.Backend.Core;
using Trader.Backend.Tools;
using Xunit;

namespace Trader.Backend.Tests;

public class ToolBehaviorTests
{
    private static ToolContext Context(int candles = 250)
        => new()
        {
            Now = DateTimeOffset.UtcNow,
            Market = MarketDataFactory.GenerateSeries("EURUSD", candles, start: 1.0850),
        };

    [Fact]
    public void TechnicalScan_ProducesBoundedScore()
    {
        var tool = new TechnicalScannerTool();
        var result = tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD", ["period"] = "14"
        }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        var score = (double)result.Data!["score"];
        Assert.InRange(score, -100, 100);
        Assert.InRange((double)result.Data["rsi"], 0, 100);
    }

    [Fact]
    public void TechnicalScan_InsufficientData_Fails()
    {
        var tool = new TechnicalScannerTool();
        var result = tool.ExecuteAsync(Context(5), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD"
        }).GetAwaiter().GetResult();

        Assert.False(result.Success);
    }

    [Fact]
    public void RiskEngine_ComputesBudgetAndFlags()
    {
        var tool = new RiskEngineTool();
        var ctx = Context();
        ctx.Portfolio.Positions.Add(new Position("EURUSD", 10_000, 1.08, 1.09));
        ctx.Portfolio.Positions.Add(new Position("BTCUSD", 0.5, 60_000, 64_000));

        var result = tool.ExecuteAsync(ctx, new Dictionary<string, string>
        {
            ["accountEquity"] = "100000", ["riskPct"] = "1.0"
        }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.Equal(1000.0, (double)result.Data!["riskBudget"]); // 1% of 100k
        Assert.True((double)result.Data["unrealizedPnl"] > 0);
    }

    [Fact]
    public void Backtester_ReturnsTradesAndReturn()
    {
        var tool = new BacktesterTool();
        var result = tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD", ["fast"] = "9", ["slow"] = "21"
        }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.True((int)result.Data!["trades"] > 0);
        Assert.InRange((double)result.Data["winRatePct"], 0, 100);
    }

    [Fact]
    public void NewsSentiment_ClassifiesBullish()
    {
        var tool = new NewsSentimentTool();
        var result = tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["text"] = "EURUSD surged on record growth, beating forecasts with a positive breakout."
        }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.Equal("bullish", result.Data!["label"]);
        Assert.True((double)result.Data["score"] > 0);
    }

    [Fact]
    public void NewsSentiment_MissingText_Fails()
    {
        var tool = new NewsSentimentTool();
        var result = tool.ExecuteAsync(Context(), new Dictionary<string, string>())
            .GetAwaiter().GetResult();
        Assert.False(result.Success);
    }

    [Fact]
    public void MarketRegime_ClassifiesTrendOrRange()
    {
        var tool = new MarketRegimeTool();
        var result = tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD"
        }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        var trend = (string)result.Data!["trend"];
        Assert.Contains(trend, new[] { "trending-up", "trending-down", "ranging" });
    }

    [Fact]
    public void PortfolioAnalyzer_EmptyPortfolio()
    {
        var tool = new PortfolioAnalyzerTool();
        var result = tool.ExecuteAsync(Context(), new Dictionary<string, string>())
            .GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.Equal(0, result.Data!["positions"]);
    }

    [Fact]
    public void Scheduler_ComputesNextRun()
    {
        var tool = new SchedulerTool();
        var ctx = Context();
        var result = tool.ExecuteAsync(ctx, new Dictionary<string, string>
        {
            ["tools"] = "tech.scan,market.regime", ["intervalMin"] = "60"
        }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.Equal(60, result.Data!["intervalMin"]);
        var next = DateTimeOffset.Parse((string)result.Data["nextRunUtc"]);
        Assert.True(next > ctx.Now);
    }
}
