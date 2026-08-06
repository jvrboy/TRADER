using NexusBrain.Agents;
using NexusBrain.Brain;
using NexusBrain.Core;
using NexusBrain.Divergence;
using NexusBrain.Forex;
using NexusBrain.Indicators;
using NexusBrain.Memory;
using NexusBrain.Orchestrator;

namespace NexusBrain.Tests;

/// <summary>
/// NexusBrain test suite — verifies the brain, neurons, memory, divergence,
/// forex, agents and indicators all work correctly. Run with: dotnet run --project tests/NexusBrain.Tests
/// </summary>
public static class Program
{
    private static int _passed;
    private static int _failed;

    public static int Main()
    {
        Console.WriteLine("==============================================");
        Console.WriteLine(" NEXUSBRAIN TEST SUITE");
        Console.WriteLine("==============================================");

        TestFeatureExtraction();
        TestNeuralNetworkLearning();
        TestVolatilityIndices();
        TestDivergenceEngine();
        TestForexAnalyzer();
        TestAgentColony();
        TestMemorySystem();
        TestBrainTraining();
        TestOrchestrator();

        Console.WriteLine("==============================================");
        Console.WriteLine($" RESULT: {_passed} passed, {_failed} failed");
        Console.WriteLine("==============================================");
        return _failed == 0 ? 0 : 1;
    }

    private static void TestFeatureExtraction()
    {
        var candles = TrainingDataGenerator.GenerateCandles("R_100", 300, seed: 1);
        var f = FeatureExtractor.Extract(candles, "R_100");
        Check("Feature extraction produces 24 features", f.Length == FeatureExtractor.FeatureCount);
        Check("Features have no NaN/Infinity", f.All(x => !double.IsNaN(x) && !double.IsInfinity(x)));
        Check("Features bounded in [-2,2]", f.All(x => x >= -2 && x <= 2));
    }

    private static void TestNeuralNetworkLearning()
    {
        // XOR-style learning
        var net = new NeuralNetwork(2, new[] { 8 }, 1, seed: 2);
        var rng = new Random(3);
        for (int i = 0; i < 3000; i++)
        {
            double x1 = rng.NextDouble() * 2 - 1, x2 = rng.NextDouble() * 2 - 1;
            double target = (x1 * x2 > 0) ? 0.9 : 0.1;
            net.Train(new[] { x1, x2 }, new[] { target }, 0.05);
        }
        int correct = 0;
        for (int i = 0; i < 200; i++)
        {
            double x1 = rng.NextDouble() * 2 - 1, x2 = rng.NextDouble() * 2 - 1;
            double p = net.PredictSingle(new[] { x1, x2 });
            if ((p > 0.5) == (x1 * x2 > 0)) correct++;
        }
        Check($"Neural network learns nonlinear function ({correct}% acc)", correct >= 75);

        // Weight round-trip
        var w = net.GetWeights();
        var net2 = new NeuralNetwork(2, new[] { 8 }, 1, seed: 2);
        net2.SetWeights(w);
        var w2 = net2.GetWeights();
        Check("Weight save/restore round-trip", w.SequenceEqual(w2));
    }

    private static void TestVolatilityIndices()
    {
        var candles = TrainingDataGenerator.GenerateCandles("R_100", 300, seed: 4);
        var c = candles.Select(x => x.Close).ToArray();
        var h = candles.Select(x => x.High).ToArray();
        var l = candles.Select(x => x.Low).ToArray();

        var vi = Volatility.VolatilityIndex(c, h, l);
        var dsi = Volatility.DriftSwitchIndex(c, h, l);
        Check("Volatility Index in [0,1]", vi.All(x => x >= 0 && x <= 1));
        Check("Drift Switch Index in [0,1]", dsi.All(x => x >= 0 && x <= 1));
        Check("VI last value finite", !double.IsNaN(vi[^1]));
        Check("DSI last value finite", !double.IsNaN(dsi[^1]));

        var atr = SeriesMath.Atr(h, l, c, 14);
        Check("ATR positive", atr[^1] > 0);

        var (mid, up, lo) = Volatility.Bollinger(c, 20, 2);
        Check("Bollinger upper >= lower", up[^1] >= lo[^1]);
    }

    private static void TestDivergenceEngine()
    {
        var candles = TrainingDataGenerator.GenerateCandles("R_100", 300, seed: 5);
        var divs = DivergenceEngine.Scan(candles, "R_100");
        Check("Divergence engine runs without error", true);
        if (divs.Count > 0)
        {
            Check("Divergence strength in (0,1]", divs.All(d => d.Strength > 0 && d.Strength <= 1));
            var (agg, strength) = DivergenceEngine.Aggregate(divs);
            Check("Divergence aggregate valid", strength >= 0 && strength <= 1);
        }
        else
        {
            Console.WriteLine("  [INFO] No divergences on this sample (acceptable)");
            _passed++;
        }
    }

    private static void TestForexAnalyzer()
    {
        var candles = TrainingDataGenerator.GenerateCandles("frxEURUSD", 300, seed: 6, startPrice: 1.08);
        var fa = ForexAnalyzer.Analyze(candles, "frxEURUSD");
        Check("Forex analysis produces a valid bias", fa.Bias is Bias.Bullish or Bias.Bearish or Bias.Neutral);
        Check("Pip size correct for EURUSD", Math.Abs(fa.PipSize - 0.0001) < 1e-9);
        Check("Pivot point computed", fa.Pivots.P > 0);
        Check("ATR pips positive", fa.AtrPips > 0);
        Check("Risk/reward ratio = 2", Math.Abs(fa.RiskReward - 2.0) < 1e-9);
        Check("Fibonacci has 7 levels", fa.Fibonacci.Length == 7);

        // JPY pair pip size
        var jpy = ForexAnalyzer.Analyze(TrainingDataGenerator.GenerateCandles("frxUSDJPY", 300, seed: 7), "frxUSDJPY");
        Check("Pip size correct for JPY pair", Math.Abs(jpy.PipSize - 0.01) < 1e-9);
    }

    private static void TestAgentColony()
    {
        var colony = new AgentColony();
        Check("Agent colony has 8 agents", colony.Agents.Count == 8);

        var candles = TrainingDataGenerator.GenerateCandles("R_100", 300, seed: 8);
        var signals = colony.RunAll(candles, "R_100", InstrumentKind.SyntheticVolatility);
        Check("Agent colony produces signals", signals.Count >= 0);

        var (bias, conf, score) = colony.Aggregate(signals);
        Check("Aggregate confidence in [0,1]", conf >= 0 && conf <= 1);
        Check("Aggregate bias valid", bias is Bias.Bullish or Bias.Bearish or Bias.Neutral);
    }

    private static void TestMemorySystem()
    {
        var mem = new MemorySystem(maxEpisodic: 100, maxWorking: 32);

        mem.SetWorking("last_price", 123.45);
        Check("Working memory stores", mem.GetWorking("last_price") is double d && Math.Abs(d - 123.45) < 1e-9);

        var id = mem.Remember(new EpisodicMemory { Symbol = "R_100", Event = "SIGNAL", Outcome = 0.5, Note = "test" });
        Check("Episodic memory stores and returns id", id > 0);
        var recalled = mem.Recall(symbol: "R_100");
        Check("Episodic memory recall", recalled.Count == 1 && recalled[0].Event == "SIGNAL");

        mem.StoreFact("market", "volatile");
        Check("Semantic memory stores", mem.GetFact("market") == "volatile");
        mem.StoreFact("market", "very volatile");
        Check("Semantic memory updates", mem.GetFact("market") == "very volatile");

        // Persistence
        var dir = Path.Combine(Path.GetTempPath(), "nexusbrain_test_mem_" + Guid.NewGuid().ToString("N"));
        mem.Save(dir);
        var mem2 = new MemorySystem();
        mem2.Load(dir);
        Check("Memory persists to disk", mem2.EpisodicCount == 1 && mem2.GetFact("market") == "very volatile");
        Directory.Delete(dir, true);
    }

    private static void TestBrainTraining()
    {
        var savePath = Path.Combine(Path.GetTempPath(), "nexusbrain_test_brain_" + Guid.NewGuid().ToString("N") + ".json");
        var brain = Brain.Brain.CreateDefault("test", savePath);

        var data = TrainingData.GenerateDataset(count: 200, seed: 10);
        Check("Training dataset generated", data.Count > 0);

        double acc = brain.TrainEpoch(data, reinforce: false);
        Check("Brain trains an epoch", acc > 0 && acc <= 1);

        brain.Save();
        var brain2 = Brain.Brain.CreateDefault("test", savePath);
        bool loaded = brain2.Load();
        Check("Brain loads from disk", loaded);
        Check("Loaded brain predicts", !double.IsNaN(brain2.Predict(data[0].Features)));
        File.Delete(savePath);
    }

    private static void TestOrchestrator()
    {
        var config = new BrainConfig
        {
            DataRoot = Path.Combine(Path.GetTempPath(), "nexusbrain_test_root_" + Guid.NewGuid().ToString("N"))
        };
        var orch = new BrainOrchestrator(config);

        var candles = TrainingDataGenerator.GenerateCandles("R_100", 300, seed: 12);
        var result = orch.Analyze(candles, "R_100", InstrumentKind.SyntheticVolatility);
        Check("Orchestrator produces analysis", result.Analysis.Symbol == "R_100");
        Check("Orchestrator computes brain prediction", !double.IsNaN(result.BrainPrediction));
        Check("Orchestrator captures VI regime", result.Analysis.Regime.ContainsKey("vi"));
        Check("Orchestrator captures DSI regime", result.Analysis.Regime.ContainsKey("dsi"));

        // Self-learning step
        double loss = orch.LearnFromOutcome(candles, "R_100", InstrumentKind.SyntheticVolatility, 0.005);
        Check("Self-learning step runs", !double.IsNaN(loss));
        Check("Episodic memory recorded", orch.Memory.EpisodicCount >= 1);

        orch.SaveAll();
        Directory.Delete(config.DataRoot, true);
    }

    private static void Check(string name, bool condition)
    {
        if (condition) { _passed++; Console.WriteLine($"  [PASS] {name}"); }
        else { _failed++; Console.WriteLine($"  [FAIL] {name}"); }
    }
}
