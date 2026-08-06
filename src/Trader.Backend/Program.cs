using Trader.Backend.Core;
using Trader.Backend.Tools;

namespace Trader.Backend;

/// <summary>
/// Agentic trading backend demo. Builds a comprehensive tool registry, wires a portfolio and
/// multi-asset market snapshot, and executes a multi-tool agent plan demonstrating all tools.
/// Run with: dotnet run --project src/Trader.Backend
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // 1. Build the tool registry with all available tools (31 tools).
        var registry = new ToolRegistry();
        registry.RegisterRange(new ITool[]
        {
            new TechnicalScannerTool(),
            new RiskEngineTool(),
            new PortfolioAnalyzerTool(),
            new BacktesterTool(),
            new NewsSentimentTool(),
            new MarketRegimeTool(),
            new SchedulerTool(),
            new CorrelationTool(),
            new VolatilityTool(),
            new SupportResistanceTool(),
            new MomentumTool(),
            new RiskRewardTool(),
            new VolumeTool(),
            new SwarmAnalyzeTool(),
            new MarketProfileTool(),
            new DrawdownTool(),
            new SharpeSortinoTool(),
            new VolSurfaceTool(),
            new SectorRotationTool(),
            new OrderFlowTool(),
            new StrategyBuilderTool(),
            new FibonacciTool(),
            new HarmonicPatternTool(),
            new MultiTimeframeTrendTool(),
            new SmartMoneyConceptTool(),
            new PivotPointsTool(),
            new PositionSizingTool(),
            new MonteCarloTool(),
            new OptionsGreeksTool(),
            new ElliottWaveTool(),
            new ArbitrageScannerTool(),
        });

        Console.WriteLine("== TRADER Agentic Backend ==");
        Console.WriteLine($"Registered {registry.All.Count} tools:\n{registry.Describe()}\n");

        // 2. Build a deterministic market snapshot and a multi-asset portfolio.
        var market = MarketDataFactory.GenerateSeries("EURUSD", 250, start: 1.0850);
        var market2 = MarketDataFactory.GenerateSeries("GBPUSD", 250, start: 1.2700);
        var tech = MarketDataFactory.GenerateSeries("TECH", 250, start: 150.0, drift: 0.0012);
        var energy = MarketDataFactory.GenerateSeries("ENERGY", 250, start: 80.0, drift: -0.0006);
        var finance = MarketDataFactory.GenerateSeries("FINANCE", 250, start: 110.0, drift: 0.0004);
        var allMarket = market.Concat(market2).Concat(tech).Concat(energy).Concat(finance).ToArray();
        var portfolio = new Portfolio();
        portfolio.Positions.Add(new Position("EURUSD", 10_000, 1.0800, market[^1].Close));
        portfolio.Positions.Add(new Position("BTCUSD", 0.5, 62_000, 64_500));

        var context = new ToolContext
        {
            Now = DateTimeOffset.UtcNow,
            Market = allMarket,
            Portfolio = portfolio,
            Log = msg => Console.WriteLine($"  {msg}"),
        };

        // 3. Create an analyst agent and run a scripted comprehensive plan.
        var agent = new Agent("analyst", registry);

        Console.WriteLine("\n== Analyst agent running full suite plan ==");
        var plan = new[]
        {
            new AgentStep("market.regime", new Dictionary<string, string> { ["symbol"] = "EURUSD" }),
            new AgentStep("tech.scan", new Dictionary<string, string> { ["symbol"] = "EURUSD", ["period"] = "14" }),
            new AgentStep("analysis.mtf", new Dictionary<string, string> { ["symbol"] = "EURUSD" }),
            new AgentStep("analysis.fibonacci", new Dictionary<string, string> { ["symbol"] = "EURUSD" }),
            new AgentStep("analysis.smc", new Dictionary<string, string> { ["symbol"] = "EURUSD" }),
            new AgentStep("analysis.harmonic", new Dictionary<string, string> { ["symbol"] = "EURUSD" }),
            new AgentStep("analysis.pivots", new Dictionary<string, string> { ["symbol"] = "EURUSD", ["type"] = "classic" }),
            new AgentStep("analysis.elliottwave", new Dictionary<string, string> { ["symbol"] = "EURUSD" }),
            new AgentStep("analysis.volatility", new Dictionary<string, string> { ["symbol"] = "EURUSD" }),
            new AgentStep("analysis.momentum", new Dictionary<string, string> { ["symbol"] = "EURUSD" }),
            new AgentStep("analysis.supplydemand", new Dictionary<string, string> { ["symbol"] = "EURUSD" }),
            new AgentStep("analysis.volume", new Dictionary<string, string> { ["symbol"] = "EURUSD" }),
            new AgentStep("analysis.correlation", new Dictionary<string, string> { ["symbolA"] = "EURUSD", ["symbolB"] = "GBPUSD" }),
            new AgentStep("analysis.arbitrage", new Dictionary<string, string> { ["symbolA"] = "EURUSD", ["symbolB"] = "GBPUSD" }),
            new AgentStep("analysis.riskreward", new Dictionary<string, string> { ["entry"] = "1.0850", ["stopLoss"] = "1.0780", ["takeProfit"] = "1.1000" }),
            new AgentStep("risk.positionsize", new Dictionary<string, string> { ["accountEquity"] = "100000", ["entry"] = "1.0850", ["stopLoss"] = "1.0780", ["model"] = "half-kelly" }),
            new AgentStep("risk.montecarlo", new Dictionary<string, string> { ["startEquity"] = "100000", ["tradesCount"] = "100", ["iterations"] = "200" }),
            new AgentStep("analysis.greeks", new Dictionary<string, string> { ["spot"] = "100", ["strike"] = "105", ["daysToExpiry"] = "30", ["volatilityPct"] = "25", ["optionType"] = "call" }),
            new AgentStep("swarm.analyze", new Dictionary<string, string> { ["symbol"] = "EURUSD" }),
            new AgentStep("analysis.marketprofile", new Dictionary<string, string> { ["symbol"] = "EURUSD" }),
            new AgentStep("analysis.drawdown", new Dictionary<string, string> { ["symbol"] = "EURUSD" }),
            new AgentStep("analysis.riskmetrics", new Dictionary<string, string> { ["symbol"] = "EURUSD", ["periodsPerYear"] = "252" }),
            new AgentStep("analysis.volsurface", new Dictionary<string, string> { ["symbol"] = "EURUSD", ["atmVol"] = "12", ["spot"] = "1.09" }),
            new AgentStep("analysis.orderflow", new Dictionary<string, string> { ["symbol"] = "EURUSD" }),
            new AgentStep("analysis.sector", new Dictionary<string, string>()),
            new AgentStep("strategy.evaluate", new Dictionary<string, string> { ["symbol"] = "EURUSD", ["rule"] = "rsi_lt_50 AND price_gt_ema9 OR ema9_gt_ema21" }),
            new AgentStep("backtest.macross", new Dictionary<string, string> { ["symbol"] = "EURUSD", ["fast"] = "9", ["slow"] = "21" }),
            new AgentStep("portfolio.summary", new Dictionary<string, string>()),
            new AgentStep("risk.assess", new Dictionary<string, string> { ["accountEquity"] = "100000", ["riskPct"] = "1.0" }),
            new AgentStep("news.sentiment", new Dictionary<string, string>
            {
                ["text"] = "EURUSD rallied on stronger growth and a positive breakout, beating forecasts."
            }),
        };

        var results = await agent.RunPlanAsync(context, plan);

        Console.WriteLine("\n== Results ==");
        foreach (var r in results)
        {
            Console.WriteLine($"  [{(r.Success ? "OK  " : "FAIL")}] {r.Message}");
        }

        // 4. Demonstrate discovery: query the scheduler for a cadence.
        var sched = await agent.RunToolAsync(context, "scheduler.plan",
            new Dictionary<string, string> { ["tools"] = "market.regime,tech.scan,analysis.mtf", ["intervalMin"] = "60" });
        Console.WriteLine($"\n  {sched.Message}");

        Console.WriteLine("\nBackend demo complete.");
        return 0;
    }
}
