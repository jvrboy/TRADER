using NexusBrain.Core;
using NexusBrain.Divergence;
using NexusBrain.Forex;
using NexusBrain.Indicators;
using NexusBrain.Orchestrator;

namespace NexusBrain.Cli;

/// <summary>
/// NexusBrain CLI — the self-learning agentic AI brain for trading.
///
/// Usage:
///   nexusbrain train            Train the brain on synthetic VI/DSI data
///   nexusbrain analyze <sym>    Run the full brain on a symbol (offline demo)
///   nexusbrain live <sym>       Pull live candles from Deriv and analyze
///   nexusbrain divergence <sym> Show divergence analysis
///   nexusbrain forex <sym>      Show forex analysis
///   nexusbrain memory           Show memory stats
///   nexusbrain test             Run the built-in self-test suite
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var cmd = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

        try
        {
            return cmd switch
            {
                "train" => RunTrain(),
                "analyze" => RunAnalyze(args.Length > 1 ? args[1] : "R_100"),
                "live" => await RunLive(args.Length > 1 ? args[1] : "R_100"),
                "divergence" => RunDivergence(args.Length > 1 ? args[1] : "R_100"),
                "forex" => RunForex(args.Length > 1 ? args[1] : "frxEURUSD"),
                "memory" => RunMemory(),
                "test" => RunSelfTest(),
                "help" or "-h" or "--help" => RunHelp(),
                _ => RunHelp()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            return 1;
        }
    }

    private static int RunHelp()
    {
        Console.WriteLine("""
        NEXUSBRAIN — Self-Learning Agentic AI Brain for Trading (native C#)
        ==================================================================
          train            Train the brain on Volatility Index + Drift Switch Index data
          analyze <sym>    Run the full brain on a symbol (offline demo)
          live <sym>       Pull live candles from the Deriv public API and analyze
          divergence <sym> Show RSI/MACD/StochRSI divergence analysis
          forex <sym>      Show the forex analysis system
          memory           Show the brain's memory stats
          test             Run the built-in self-test suite
        """);
        return 0;
    }

    private static int RunTrain()
    {
        Console.WriteLine("=== NEXUSBRAIN TRAINING PIPELINE ===");
        var cfg = new BrainConfig();
        var orch = new BrainOrchestrator(cfg);

        Console.WriteLine("Generating Volatility Index (VI) + Drift Switch Index (DSI) training data...");
        // Larger, diverse dataset across multiple regime seeds
        var allData = new List<(double[] Features, double Target)>();
        foreach (var seed in new[] { 42, 7, 123, 2024, 555, 9999, 31337, 8080 })
            allData.AddRange(TrainingData.GenerateDataset(count: 400, seed: seed));
        Console.WriteLine($"Generated {allData.Count} labelled samples across 8 regime seeds.");

        // Train/validation split (80/20)
        var rng = new Random(1);
        var shuffled = allData.OrderBy(_ => rng.Next()).ToList();
        int split = (int)(shuffled.Count * 0.8);
        var trainSet = shuffled.Take(split).ToList();
        var valSet = shuffled.Skip(split).ToList();

        Console.WriteLine($"Training on {trainSet.Count} samples, validating on {valSet.Count}...");
        double bestAcc = 0;
        for (int epoch = 0; epoch < 20; epoch++)
        {
            double acc = orch.Brain.TrainEpoch(trainSet, reinforce: false);
            orch.Brain.Replay(32);
            bestAcc = Math.Max(bestAcc, acc);
        }
        Console.WriteLine($"Best training accuracy: {bestAcc:P1}");

        // Validate
        int vCorrect = 0;
        foreach (var (f, t) in valSet)
        {
            bool up = orch.Brain.Predict(f) > 0;
            if (up == (t > 0)) vCorrect++;
        }
        double valAcc = (double)vCorrect / valSet.Count;
        Console.WriteLine($"Validation accuracy: {valAcc:P1}");

        // Self-learning reinforcement pass (reward = realised outcome)
        Console.WriteLine("Running self-learning reinforcement pass...");
        var reinforceData = TrainingData.GenerateDataset(count: 400, seed: 7);
        foreach (var (f, t) in reinforceData)
        {
            double reward = Math.Clamp(t, -1, 1); // reward proportional to outcome
            orch.Brain.TrainReinforced(f, t, reward);
        }
        orch.Brain.Replay(32);
        orch.Brain.Save();
        Console.WriteLine($"Brain saved. Epochs: {orch.Brain.Epoch}, accuracy: {orch.Brain.TrainingAccuracy:P1}");

        // Verify on fresh holdout
        var holdout = TrainingData.GenerateDataset(count: 400, seed: 99);
        int correct = 0;
        foreach (var (f, t) in holdout)
        {
            bool up = orch.Brain.Predict(f) > 0;
            bool expUp = t > 0;
            if (up == expUp) correct++;
        }
        double holdoutAcc = (double)correct / holdout.Count;
        Console.WriteLine($"Holdout accuracy: {holdoutAcc:P1}");
        orch.Memory.StoreFact("training", $"trained on VI+DSI, epoch {orch.Brain.Epoch}, val {valAcc:P0}, holdout {holdoutAcc:P0}");
        orch.SaveAll();
        return 0;
    }

    private static int RunAnalyze(string symbol)
    {
        Console.WriteLine($"=== BRAIN ANALYSIS: {symbol} ===");
        var orch = new BrainOrchestrator();
        var candles = TrainingDataGenerator.GenerateCandles(symbol, 300, seed: 123);
        var kind = Classify(symbol);
        var result = orch.Analyze(candles, symbol, kind);
        PrintAnalysis(result);
        return 0;
    }

    private static async Task<int> RunLive(string symbol)
    {
        Console.WriteLine($"=== LIVE ANALYSIS: {symbol} (Deriv public API) ===");
        var orch = new BrainOrchestrator();
        Console.Write("Testing Deriv connection... ");
        bool ok = await orch.TestDerivAsync();
        Console.WriteLine(ok ? "OK" : "FAILED (offline mode)");
        var result = await orch.AnalyzeLiveAsync(symbol, Classify(symbol), 60, 300);
        if (result is null)
        {
            Console.WriteLine("Could not fetch enough candles (need live internet or more history). Falling back to demo data.");
            return RunAnalyze(symbol);
        }
        PrintAnalysis(result);
        return 0;
    }

    private static int RunDivergence(string symbol)
    {
        Console.WriteLine($"=== DIVERGENCE ANALYSIS: {symbol} ===");
        var candles = TrainingDataGenerator.GenerateCandles(symbol, 300, seed: 123);
        var divs = DivergenceEngine.Scan(candles, symbol);
        if (divs.Count == 0)
        {
            Console.WriteLine("No divergences detected on the latest bars.");
            return 0;
        }
        foreach (var d in divs.OrderByDescending(x => x.Strength).Take(10))
        {
            Console.WriteLine($"  {d.Type,-20} on {d.Indicator,-10} strength={d.Strength:P0}  (price pivot @ bar {d.PricePivotIndex})");
        }
        var (agg, strength) = DivergenceEngine.Aggregate(divs);
        Console.WriteLine($"\nAggregate: {agg} (strength {strength:P0})");
        return 0;
    }

    private static int RunForex(string symbol)
    {
        Console.WriteLine($"=== FOREX ANALYSIS: {symbol} ===");
        double start = symbol.ToUpperInvariant().EndsWith("JPY") ? 150.0 : 1.08;
        var candles = TrainingDataGenerator.GenerateCandles(symbol, 300, seed: 123, startPrice: start);
        var fa = ForexAnalyzer.Analyze(candles, symbol);
        Console.WriteLine($"  Price:       {fa.LastPrice:F5}  (pip size {fa.PipSize})");
        Console.WriteLine($"  Bias:        {fa.Bias}  confidence {fa.Confidence:P0}");
        Console.WriteLine($"  Pivots:      P={fa.Pivots.P:F5} R1={fa.Pivots.R1:F5} S1={fa.Pivots.S1:F5}");
        Console.WriteLine($"  Support:     {string.Join(", ", fa.SupportLevels.Select(x => x.ToString("F5")))}");
        Console.WriteLine($"  Resistance:  {string.Join(", ", fa.ResistanceLevels.Select(x => x.ToString("F5")))}");
        Console.WriteLine($"  Candle:      {fa.CandlePattern} ({fa.CandleBias})");
        Console.WriteLine($"  ATR:         {fa.AtrPips:F1} pips  Stop {fa.SuggestedStopPips:F0}p  Target {fa.SuggestedTargetPips:F0}p  RR 1:{fa.RiskReward}");
        foreach (var note in fa.Notes) Console.WriteLine($"  - {note}");
        return 0;
    }

    private static int RunMemory()
    {
        var orch = new BrainOrchestrator();
        Console.WriteLine("=== BRAIN MEMORY ===");
        Console.WriteLine($"  Episodic memories: {orch.Memory.EpisodicCount}");
        Console.WriteLine($"  Semantic facts:    {orch.Memory.SemanticCount}");
        Console.WriteLine($"  Working slots:     {orch.Memory.WorkingSnapshot().Count()}");
        Console.WriteLine($"  Brain epochs:      {orch.Brain.Epoch}");
        Console.WriteLine($"  Brain accuracy:    {orch.Brain.TrainingAccuracy:P1}");
        foreach (var f in orch.Memory.AllFacts().Take(10))
            Console.WriteLine($"  - [{f.Key}] = {f.Value}");
        return 0;
    }

    private static int RunSelfTest()
    {
        Console.WriteLine("=== NEXUSBRAIN SELF-TEST SUITE ===");
        int passed = 0, failed = 0;

        // 1. Feature extraction
        try
        {
            var candles = TrainingDataGenerator.GenerateCandles("R_100", 300, seed: 1);
            var f = FeatureExtractor.Extract(candles);
            if (f.Length == FeatureExtractor.FeatureCount && f.All(x => !double.IsNaN(x)))
            { Console.WriteLine("  [PASS] Feature extraction (24 features, no NaN)"); passed++; }
            else { Console.WriteLine("  [FAIL] Feature extraction"); failed++; }
        }
        catch (Exception ex) { Console.WriteLine($"  [FAIL] Feature extraction: {ex.Message}"); failed++; }

        // 2. Neural network learns a simple function
        try
        {
            var net = new Brain.NeuralNetwork(2, new[] { 8 }, 1, seed: 2);
            var rng = new Random(3);
            for (int i = 0; i < 2000; i++)
            {
                double x1 = rng.NextDouble() * 2 - 1, x2 = rng.NextDouble() * 2 - 1;
                double target = (x1 * x2 > 0) ? 0.9 : 0.1;
                net.Train(new[] { x1, x2 }, new[] { target }, 0.05);
            }
            int correct = 0;
            for (int i = 0; i < 100; i++)
            {
                double x1 = rng.NextDouble() * 2 - 1, x2 = rng.NextDouble() * 2 - 1;
                double p = net.PredictSingle(new[] { x1, x2 });
                bool exp = x1 * x2 > 0;
                if ((p > 0.5) == exp) correct++;
            }
            if (correct >= 80)
            { Console.WriteLine($"  [PASS] Neural network learns XOR-like function ({correct}% acc)"); passed++; }
            else { Console.WriteLine($"  [FAIL] Neural network learning ({correct}% acc)"); failed++; }
        }
        catch (Exception ex) { Console.WriteLine($"  [FAIL] Neural network: {ex.Message}"); failed++; }

        // 3. Volatility Index + Drift Switch Index produce sane ranges
        try
        {
            var candles = TrainingDataGenerator.GenerateCandles("R_100", 300, seed: 4);
            var c = candles.Select(x => x.Close).ToArray();
            var h = candles.Select(x => x.High).ToArray();
            var l = candles.Select(x => x.Low).ToArray();
            var vi = Volatility.VolatilityIndex(c, h, l);
            var dsi = Volatility.DriftSwitchIndex(c, h, l);
            bool viOk = vi.All(x => x >= 0 && x <= 1);
            bool dsiOk = dsi.All(x => x >= 0 && x <= 1);
            if (viOk && dsiOk)
            { Console.WriteLine($"  [PASS] VI/DSI in [0,1] (VI={vi[^1]:P0}, DSI={dsi[^1]:P0})"); passed++; }
            else { Console.WriteLine("  [FAIL] VI/DSI range"); failed++; }
        }
        catch (Exception ex) { Console.WriteLine($"  [FAIL] VI/DSI: {ex.Message}"); failed++; }

        // 4. Divergence engine
        try
        {
            var candles = TrainingDataGenerator.GenerateCandles("R_100", 300, seed: 5);
            var divs = DivergenceEngine.Scan(candles, "R_100");
            Console.WriteLine($"  [PASS] Divergence engine ran ({divs.Count} divergences)"); passed++;
        }
        catch (Exception ex) { Console.WriteLine($"  [FAIL] Divergence: {ex.Message}"); failed++; }

        // 5. Forex analysis
        try
        {
            var candles = TrainingDataGenerator.GenerateCandles("frxEURUSD", 300, seed: 6, startPrice: 1.08);
            var fa = ForexAnalyzer.Analyze(candles, "frxEURUSD");
            if (fa.Bias is Bias.Bullish or Bias.Bearish or Bias.Neutral)
            { Console.WriteLine($"  [PASS] Forex analysis (bias {fa.Bias})"); passed++; }
            else { Console.WriteLine("  [FAIL] Forex analysis"); failed++; }
        }
        catch (Exception ex) { Console.WriteLine($"  [FAIL] Forex: {ex.Message}"); failed++; }

        // 6. Agent colony
        try
        {
            var orch = new BrainOrchestrator();
            var candles = TrainingDataGenerator.GenerateCandles("R_100", 300, seed: 8);
            var result = orch.Analyze(candles, "R_100", InstrumentKind.SyntheticVolatility);
            Console.WriteLine($"  [PASS] Agent colony produced {result.SignalsProduced} signals"); passed++;
        }
        catch (Exception ex) { Console.WriteLine($"  [FAIL] Colony: {ex.Message}"); failed++; }

        // 7. Memory system
        try
        {
            var orch = new BrainOrchestrator();
            orch.Memory.Remember(new Memory.EpisodicMemory { Symbol = "R_100", Event = "TEST", Outcome = 0.5 });
            var recalled = orch.Memory.Recall(symbol: "R_100");
            if (recalled.Count > 0)
            { Console.WriteLine("  [PASS] Memory store + recall"); passed++; }
            else { Console.WriteLine("  [FAIL] Memory recall"); failed++; }
        }
        catch (Exception ex) { Console.WriteLine($"  [FAIL] Memory: {ex.Message}"); failed++; }

        Console.WriteLine($"\n=== RESULT: {passed} passed, {failed} failed ===");
        return failed == 0 ? 0 : 1;
    }

    private static void PrintAnalysis(BrainRunResult result)
    {
        var a = result.Analysis;
        Console.WriteLine($"  Aggregate bias: {a.AggregateBias}  confidence {a.AggregateConfidence:P0}");
        Console.WriteLine($"  Brain prediction: {result.BrainPrediction:+0.00;-0.00} ({(result.BrainPrediction >= 0 ? "bullish" : "bearish")})");
        Console.WriteLine($"  Volatility Index: {a.Regime.GetValueOrDefault("vi"):P0}  ({a.Notes.GetValueOrDefault("vi")})");
        Console.WriteLine($"  Drift Switch:     {a.Regime.GetValueOrDefault("dsi"):P0}  ({a.Notes.GetValueOrDefault("dsi")})");
        Console.WriteLine($"  Signals ({result.SignalsProduced}):");
        foreach (var s in a.Signals)
            Console.WriteLine($"    - {s}");
        if (result.Divergences.Count > 0)
        {
            Console.WriteLine($"  Divergences ({result.Divergences.Count}):");
            foreach (var d in result.Divergences.OrderByDescending(x => x.Strength).Take(5))
                Console.WriteLine($"    - {d.Type} on {d.Indicator} ({d.Strength:P0})");
        }
        Console.WriteLine($"  Memory: {result.EpisodicMemoryCount} episodic, {result.SemanticMemoryCount} semantic");
    }

    private static InstrumentKind Classify(string symbol)
    {
        var s = symbol.ToUpperInvariant();
        if (s.StartsWith("FRX")) return InstrumentKind.Forex;
        if (s.StartsWith("R_") || s.Contains("V")) return InstrumentKind.SyntheticVolatility;
        if (s.Contains("HZ")) return InstrumentKind.SyntheticDriftSwitch;
        return InstrumentKind.SyntheticVolatility;
    }
}
