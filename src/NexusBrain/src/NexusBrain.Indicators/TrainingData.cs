using NexusBrain.Core;

namespace NexusBrain.Indicators;

/// <summary>
/// Training-data helpers that build labelled (features, target) datasets from
/// synthetic Volatility Index / Drift Switch Index candles for offline training.
/// </summary>
public static class TrainingData
{
    /// <summary>
    /// Generate a labelled dataset of (features, target) pairs from synthetic
    /// VI/DSI candles. Target is the normalised forward return in [-1, 1].
    /// </summary>
    public static List<(double[] Features, double Target)> GenerateDataset(int count = 300, int lookAhead = 5, int? seed = null)
    {
        var result = new List<(double[], double)>();
        var candles = TrainingDataGenerator.GenerateCandles("R_100", count + 80, seed);
        for (int i = 80; i < count + 80; i++)
        {
            var window = candles[(i - 80)..i];
            if (window.Length < 40) continue;
            try
            {
                var f = FeatureExtractor.Extract(window, "R_100");
                double future = i + lookAhead < candles.Length ? candles[i + lookAhead].Close : candles[i].Close;
                double pct = (future - candles[i].Close) / candles[i].Close;
                double target = Math.Clamp(pct / 0.01, -1, 1);
                result.Add((f, target));
            }
            catch { /* skip edge windows */ }
        }
        return result;
    }
}
