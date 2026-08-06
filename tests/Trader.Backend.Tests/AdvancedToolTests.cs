using Trader.Backend.Core;
using Trader.Backend.Tools;
using Xunit;

namespace Trader.Backend.Tests;

public class AdvancedToolTests
{
    private static ToolContext Context(int candles = 250)
        => new()
        {
            Now = DateTimeOffset.UtcNow,
            Market = MarketDataFactory.GenerateSeries("EURUSD", candles, start: 1.0850)
                .Concat(MarketDataFactory.GenerateSeries("GBPUSD", candles, start: 1.2700))
                .ToArray(),
        };

    [Fact]
    public async Task FibonacciTool_CalculatesLevelsAndNearestProximity()
    {
        var tool = new FibonacciTool();
        var result = await tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD",
            ["lookback"] = "50"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.ContainsKey("levels"));
        Assert.True((double)result.Data["swingHigh"] >= (double)result.Data["swingLow"]);
        Assert.InRange((double)result.Data["distancePct"], 0.0, 100.0);
    }

    [Fact]
    public async Task HarmonicPatternTool_DetectsPatternsAndTargets()
    {
        var tool = new HarmonicPatternTool();
        var result = await tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD",
            ["lookback"] = "60"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEmpty((string)result.Data["pattern"]);
        Assert.True((double)result.Data["confidencePct"] > 0);
        Assert.True((double)result.Data["prz"] > 0);
    }

    [Fact]
    public async Task MultiTimeframeTrendTool_EvaluatesConfluence()
    {
        var tool = new MultiTimeframeTrendTool();
        var result = await tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        var confluence = (double)result.Data["confluencePct"];
        Assert.InRange(confluence, 0.0, 100.0);
        Assert.NotEmpty((string)result.Data["consensus"]);
    }

    [Fact]
    public async Task SmartMoneyConceptTool_IdentifiesZonesAndFvgs()
    {
        var tool = new SmartMoneyConceptTool();
        var result = await tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD",
            ["lookback"] = "50"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEmpty((string)result.Data["pricingZone"]);
        Assert.True((double)result.Data["equilibrium"] > 0);
        Assert.True((int)result.Data["totalFvgCount"] >= 0);
    }

    [Fact]
    public async Task PivotPointsTool_CalculatesMultipleModes()
    {
        var tool = new PivotPointsTool();
        foreach (var mode in new[] { "classic", "fibonacci", "camarilla", "woodie" })
        {
            var result = await tool.ExecuteAsync(Context(), new Dictionary<string, string>
            {
                ["symbol"] = "EURUSD",
                ["type"] = mode
            });

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.True((double)result.Data["pivot"] > 0);
            var levels = (Dictionary<string, double>)result.Data["levels"];
            Assert.NotEmpty(levels);
        }
    }

    [Fact]
    public async Task PositionSizingTool_ComputesKellyAndFixedRisk()
    {
        var tool = new PositionSizingTool();
        var result = await tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["accountEquity"] = "100000",
            ["entry"] = "1.0850",
            ["stopLoss"] = "1.0780",
            ["model"] = "half-kelly"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True((double)result.Data["units"] > 0);
        Assert.True((double)result.Data["capitalExposure"] > 0);
        Assert.True((double)result.Data["maxRiskDollars"] > 0);
    }

    [Fact]
    public async Task MonteCarloTool_RunsPathsAndOutputsPercentiles()
    {
        var tool = new MonteCarloTool();
        var result = await tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["startEquity"] = "50000",
            ["tradesCount"] = "50",
            ["iterations"] = "100"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True((double)result.Data["medianEndingEquity"] > 0);
        Assert.True((double)result.Data["percentile90Equity"] >= (double)result.Data["percentile10Equity"]);
        Assert.InRange((double)result.Data["ruinProbabilityPct"], 0.0, 100.0);
    }

    [Fact]
    public async Task OptionsGreeksTool_ComputesBlackScholesAndGreeks()
    {
        var tool = new OptionsGreeksTool();
        var resultCall = await tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["spot"] = "100",
            ["strike"] = "100",
            ["daysToExpiry"] = "30",
            ["volatilityPct"] = "20",
            ["optionType"] = "call"
        });

        Assert.True(resultCall.Success);
        Assert.NotNull(resultCall.Data);
        Assert.True((double)resultCall.Data["price"] > 0);
        Assert.InRange((double)resultCall.Data["delta"], 0.40, 0.65);
        Assert.True((double)resultCall.Data["gamma"] > 0);
        Assert.True((double)resultCall.Data["thetaPerDay"] < 0);
        Assert.True((double)resultCall.Data["vegaPerPct"] > 0);

        var resultPut = await tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["spot"] = "100",
            ["strike"] = "100",
            ["daysToExpiry"] = "30",
            ["volatilityPct"] = "20",
            ["optionType"] = "put"
        });

        Assert.True(resultPut.Success);
        Assert.NotNull(resultPut.Data);
        Assert.InRange((double)resultPut.Data["delta"], -0.65, -0.35);
    }

    [Fact]
    public async Task ElliottWaveTool_IdentifiesCyclePhases()
    {
        var tool = new ElliottWaveTool();
        var result = await tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbol"] = "EURUSD"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEmpty((string)result.Data["phase"]);
        Assert.NotEmpty((string)result.Data["recommendation"]);
    }

    [Fact]
    public async Task ArbitrageScannerTool_ComputesZScoreAndSignal()
    {
        var tool = new ArbitrageScannerTool();
        var result = await tool.ExecuteAsync(Context(), new Dictionary<string, string>
        {
            ["symbolA"] = "EURUSD",
            ["symbolB"] = "GBPUSD",
            ["lookback"] = "50"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEmpty((string)result.Data["signal"]);
        Assert.True((double)result.Data["hedgeRatioBeta"] != 0);
    }
}
