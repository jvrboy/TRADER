using Brain.Core.Ensemble;

namespace Brain.Training;

/// <summary>
/// Validates the trained ensemble using cross-validation and performance metrics.
/// </summary>
public sealed class ModelValidator
{
    /// <summary>
    /// Computes accuracy, F1 score, and drift detection latency.
    /// </summary>
    public ValidationMetrics Validate(NeuralEnsemble ensemble, List<DriftSample> testData, int driftIndex)
    {
        var truePos = 0; var falsePos = 0; var trueNeg = 0; var falseNeg = 0;
        var latencies = new List<long>();

        foreach (var sample in testData)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var prediction = ensemble.Predict(sample.Features, driftIndex);
            sw.Stop();
            latencies.Add(sw.ElapsedMilliseconds);

            var predicted = prediction.Direction > 0 ? 1f : 0f;
            var actual = sample.Direction;

            if (predicted == 1 && actual == 1) truePos++;
            else if (predicted == 1 && actual == 0) falsePos++;
            else if (predicted == 0 && actual == 0) trueNeg++;
            else falseNeg++;
        }

        var accuracy = (float)(truePos + trueNeg) / testData.Count;
        var precision = truePos + falsePos > 0 ? (float)truePos / (truePos + falsePos) : 0;
        var recall = truePos + falseNeg > 0 ? (float)truePos / (truePos + falseNeg) : 0;
        var f1 = precision + recall > 0 ? 2 * precision * recall / (precision + recall) : 0;

        return new ValidationMetrics
        {
            Accuracy = accuracy,
            Precision = precision,
            Recall = recall,
            F1Score = f1,
            AverageLatencyMs = latencies.Average(),
            MaxLatencyMs = latencies.Max(),
            TotalSamples = testData.Count
        };
    }
}

public sealed class ValidationMetrics
{
    public float Accuracy { get; init; }
    public float Precision { get; init; }
    public float Recall { get; init; }
    public float F1Score { get; init; }
    public double AverageLatencyMs { get; init; }
    public long MaxLatencyMs { get; init; }
    public int TotalSamples { get; init; }
}
