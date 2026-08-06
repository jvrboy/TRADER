using System.Text.Json;
using NexusBrain.Core;
using NexusBrain.Indicators;

namespace NexusBrain.Brain;

/// <summary>
/// The agentic AI brain: a neural network + self-learning engine + training
/// pipeline. Learns to predict market direction from feature vectors, trains on
/// the Volatility Index and Drift Switch Index, and persists its weights.
/// </summary>
public sealed class Brain
{
    public string Name { get; }
    public int InputDim { get; }
    public NeuralNetwork Network { get; }
    public SelfLearningEngine Learner { get; }
    public int Epoch { get; private set; }
    public double TrainingAccuracy { get; private set; }

    private readonly string _savePath;
    private readonly Random _rng;

    public Brain(string name, int inputDim, string savePath, int hiddenUnits = 64, int? seed = null)
    {
        Name = name;
        InputDim = inputDim;
        _savePath = savePath;
        _rng = seed is null ? new Random() : new Random(seed.Value);
        Network = new NeuralNetwork(inputDim, new[] { hiddenUnits, hiddenUnits / 2 }, 1,
            Activation.Tanh, Activation.Sigmoid, momentum: 0.9, seed: seed);
        Learner = new SelfLearningEngine(Network, initialLr: 0.02, seed: seed);
        Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
    }

    /// <summary>Predict market direction score in [-1, 1]. Positive = bullish.</summary>
    public double Predict(double[] features)
        => (Network.PredictSingle(features) - 0.5) * 2.0;

    /// <summary>Map a prediction score to a bias label.</summary>
    public Bias ToBias(double score)
        => score > 0.1 ? Bias.Bullish : score < -0.1 ? Bias.Bearish : Bias.Neutral;

    /// <summary>Train on a single labelled sample (features + target in [-1,1]).</summary>
    public double Train(double[] features, double target)
        => Learner.Supervise(features, target, Learner.LearningRate);

    /// <summary>Reinforcement training with a reward signal.</summary>
    public double TrainReinforced(double[] features, double target, double reward)
        => Learner.Reinforce(features, target, reward);

    /// <summary>Replay a batch from memory.</summary>
    public double Replay(int batch = 32) => Learner.Replay(batch);

    /// <summary>
    /// Run a full training epoch over a dataset of (features, target) pairs.
    /// Returns classification accuracy on the dataset.
    /// </summary>
    public double TrainEpoch(IEnumerable<(double[] Features, double Target)> dataset, bool reinforce = false)
    {
        var data = dataset.ToList();
        if (data.Count == 0) return 0;
        foreach (var (f, t) in data)
        {
            if (reinforce)
            {
                double reward = t > 0 ? 1.0 : -1.0;
                Learner.Reinforce(f, t, reward);
            }
            else Learner.Supervise(f, t, Learner.LearningRate);
        }
        Learner.Replay(Math.Min(32, data.Count));
        Epoch++;
        // Evaluate accuracy
        int correct = 0;
        foreach (var (f, t) in data)
        {
            double p = Predict(f);
            bool expUp = t > 0;
            bool predUp = p > 0;
            if (expUp == predUp) correct++;
        }
        TrainingAccuracy = (double)correct / data.Count;
        return TrainingAccuracy;
    }

    /// <summary>Save the brain weights + metadata to disk.</summary>
    public void Save()
    {
        var payload = new
        {
            name = Name,
            inputDim = InputDim,
            epoch = Epoch,
            accuracy = TrainingAccuracy,
            trainSteps = Learner.TrainSteps,
            cumulativeReward = Learner.CumulativeReward,
            learningRate = Learner.LearningRate,
            weights = Network.GetWeights()
        };
        File.WriteAllText(_savePath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>Load brain weights from disk. Returns true if loaded.</summary>
    public bool Load()
    {
        if (!File.Exists(_savePath)) return false;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(_savePath));
            var root = doc.RootElement;
            if (root.TryGetProperty("weights", out var w) && w.ValueKind == JsonValueKind.Array)
            {
                var arr = w.EnumerateArray().Select(x => x.GetDouble()).ToArray();
                if (arr.Length == Network.ParameterCount)
                {
                    Network.SetWeights(arr);
                    if (root.TryGetProperty("epoch", out var ep)) Epoch = ep.GetInt32();
                    if (root.TryGetProperty("accuracy", out var acc)) TrainingAccuracy = acc.GetDouble();
                    return true;
                }
            }
        }
        catch { /* corrupt file — start fresh */ }
        return false;
    }

    /// <summary>Static helper: build a brain pre-trained with a deterministic seed.</summary>
    public static Brain CreateDefault(string name, string savePath, int hiddenUnits = 64)
        => new(name, FeatureExtractor.FeatureCount, savePath, hiddenUnits, seed: 42);
}
