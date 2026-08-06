using System.Collections.Concurrent;
using Brain.Core.Networks;

namespace Brain.Core.Ensemble;

/// <summary>
/// Manages 1000+ neural networks and aggregates their predictions.
/// Uses weighted voting based on each network's validation accuracy.
/// Parallel inference ensures sub-200ms prediction latency.
/// </summary>
public sealed class NeuralEnsemble
{
    private readonly List<NeuralNetworkBase> _networks = new();
    private readonly object _lock = new();
    public int Count => _networks.Count;

    public void AddNetwork(NeuralNetworkBase network) { lock (_lock) _networks.Add(network); }

    public IReadOnlyList<NeuralNetworkBase> Networks
    {
        get { lock (_lock) return _networks.ToArray(); }
    }

    /// <summary>
    /// Runs prediction across all networks in parallel and aggregates results.
    /// Target: 1000+ networks in under 200ms.
    /// </summary>
    public EnsemblePrediction Predict(float[] input, int driftIndex)
    {
        var networks = Networks;
        var results = new ConcurrentBag<(float[] output, float weight)>();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        Parallel.ForEach(networks, options, network =>
        {
            try
            {
                var output = network.Forward(input);
                results.Add((output, network.ValidationAccuracy));
            }
            catch
            {
            }
        });

        var totalWeight = 0f;
        var sumDirection = 0f;
        var sumMagnitude = 0f;
        var upVotes = 0;
        var downVotes = 0;

        foreach (var (output, weight) in results)
        {
            var direction = output[0];
            var magnitude = output.Length > 1 ? output[1] : MathF.Abs(direction);

            sumDirection += direction * weight;
            sumMagnitude += magnitude * weight;
            totalWeight += weight;

            if (direction > 0) upVotes++;
            else downVotes++;
        }

        if (totalWeight == 0) totalWeight = 1;

        return new EnsemblePrediction
        {
            DriftIndex = driftIndex,
            Direction = sumDirection / totalWeight,
            Magnitude = sumMagnitude / totalWeight,
            Confidence = (float)Math.Max(upVotes, downVotes) / networks.Count,
            NetworkCount = networks.Count,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Saves all network weights to a directory.
    /// </summary>
    public void SaveAll(string directory)
    {
        Directory.CreateDirectory(directory);
        var networks = Networks;
        for (int i = 0; i < networks.Count; i++)
        {
            var path = Path.Combine(directory, "ensemble_weights_" + i.ToString("D4") + ".bin");
            using var fs = File.Create(path);
            using var writer = new BinaryWriter(fs);
            networks[i].Save(writer);
        }
    }

    /// <summary>
    /// Updates validation accuracy for a network by ID.
    /// </summary>
    public void UpdateAccuracy(Guid networkId, float accuracy)
    {
        lock (_lock)
        {
            var network = _networks.FirstOrDefault(n => n.Id == networkId);
            if (network != null)
                network.ValidationAccuracy = accuracy;
        }
    }
}

public sealed record EnsemblePrediction
{
    public int DriftIndex { get; init; }
    public float Direction { get; init; }
    public float Magnitude { get; init; }
    public float Confidence { get; init; }
    public int NetworkCount { get; init; }
    public DateTime Timestamp { get; init; }
}
