using Brain.Core.Ensemble;
using Brain.Core.Networks;

namespace Brain.Training;

/// <summary>
/// Trains the neural ensemble on drift switch indices (10, 20, 30).
/// Each network is trained independently with early stopping.
/// </summary>
public sealed class EnsembleTrainer
{
    private readonly NeuralEnsemble _ensemble;
    private readonly DerivApiClient _apiClient;

    public EnsembleTrainer(NeuralEnsemble ensemble, DerivApiClient apiClient)
    {
        _ensemble = ensemble;
        _apiClient = apiClient;
    }

    /// <summary>
    /// Trains the ensemble on the specified drift indices.
    /// </summary>
    public async Task<TrainingResult> TrainAsync(int[] driftIndices, int epochs = 10, float learningRate = 0.001f)
    {
        var result = new TrainingResult { DriftIndices = driftIndices };
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        foreach (var index in driftIndices)
        {
            var symbol = "R_" + index;
            var ticks = await _apiClient.GetTicksAsync(symbol, 2000);
            var samples = DriftSwitchDataPreparer.Prepare(ticks, index);
            var (train, val, test) = DriftSwitchDataPreparer.Split(samples);

            var indexResult = new IndexTrainingResult { DriftIndex = index, SampleCount = samples.Count };

            // Train each network
            var networks = _ensemble.Networks;
            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            Parallel.ForEach(networks, options, network =>
            {
                var accuracy = TrainNetwork(network, train, val, epochs, learningRate);
                _ensemble.UpdateAccuracy(network.Id, accuracy);
            });

            // Evaluate on test set
            var testAccuracy = EvaluateEnsemble(_ensemble, test, index);
            indexResult.TestAccuracy = testAccuracy;
            indexResult.ValidationAccuracy = EvaluateEnsemble(_ensemble, val, index);

            result.IndexResults.Add(indexResult);
        }

        stopwatch.Stop();
        result.TrainingTimeMs = stopwatch.ElapsedMilliseconds;
        result.Success = true;

        // Save trained weights
        _ensemble.SaveAll(Path.Combine(AppContext.BaseDirectory, "models"));

        return result;
    }

    /// <summary>
    /// Trains a single network with early stopping.
    /// </summary>
    private float TrainNetwork(NeuralNetworkBase network, List<DriftSample> train, List<DriftSample> val,
        int epochs, float learningRate)
    {
        var bestAccuracy = 0f;
        var patience = 3;
        var noImprovement = 0;

        for (int epoch = 0; epoch < epochs; epoch++)
        {
            // Shuffle training data
            var rng = new Random(epoch);
            var shuffled = train.OrderBy(_ => rng.NextDouble()).ToList();

            foreach (var sample in shuffled)
            {
                var target = new float[] { sample.Direction, sample.Magnitude };
                network.Backward(sample.Features, target, learningRate);
            }

            // Validate
            var accuracy = EvaluateNetwork(network, val);
            if (accuracy > bestAccuracy)
            {
                bestAccuracy = accuracy;
                noImprovement = 0;
            }
            else
            {
                noImprovement++;
                if (noImprovement >= patience) break;
            }
        }

        return bestAccuracy;
    }

    private float EvaluateNetwork(NeuralNetworkBase network, List<DriftSample> data)
    {
        if (data.Count == 0) return 0;
        var correct = 0;
        foreach (var sample in data)
        {
            var output = network.Forward(sample.Features);
            var predicted = output[0] > 0.5 ? 1f : 0f;
            if (predicted == sample.Direction) correct++;
        }
        return (float)correct / data.Count;
    }

    private float EvaluateEnsemble(NeuralEnsemble ensemble, List<DriftSample> data, int driftIndex)
    {
        if (data.Count == 0) return 0;
        var correct = 0;
        foreach (var sample in data)
        {
            var prediction = ensemble.Predict(sample.Features, driftIndex);
            var predicted = prediction.Direction > 0 ? 1f : 0f;
            if (predicted == sample.Direction) correct++;
        }
        return (float)correct / data.Count;
    }
}

public sealed class TrainingResult
{
    public bool Success { get; set; }
    public int[] DriftIndices { get; set; } = Array.Empty<int>();
    public long TrainingTimeMs { get; set; }
    public List<IndexTrainingResult> IndexResults { get; set; } = new();
}

public sealed class IndexTrainingResult
{
    public int DriftIndex { get; init; }
    public int SampleCount { get; init; }
    public float ValidationAccuracy { get; set; }
    public float TestAccuracy { get; set; }
}
