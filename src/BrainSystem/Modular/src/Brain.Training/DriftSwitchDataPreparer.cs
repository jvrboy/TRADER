namespace Brain.Training;

/// <summary>
/// Prepares training data for drift switch indices (10, 20, 30).
/// Computes drift as logarithmic return, normalizes features, and splits data.
/// </summary>
public static class DriftSwitchDataPreparer
{
    /// <summary>
    /// Computes drift (logarithmic return) over a fixed window.
    /// </summary>
    public static List<DriftSample> Prepare(List<TickData> ticks, int windowSize = 10)
    {
        var samples = new List<DriftSample>();

        for (int i = windowSize; i < ticks.Count; i++)
        {
            var window = ticks.Skip(i - windowSize).Take(windowSize).ToList();
            var features = ComputeFeatures(window);
            var logReturn = Math.Log(ticks[i].Price / ticks[i - 1].Price);
            var direction = logReturn > 0 ? 1f : 0f;
            var magnitude = (float)Math.Abs(logReturn);

            samples.Add(new DriftSample
            {
                Features = features,
                Direction = direction,
                Magnitude = magnitude,
                Timestamp = ticks[i].Timestamp
            });
        }

        return Normalize(samples);
    }

    /// <summary>
    /// Computes technical indicators from a window of tick data.
    /// Features: normalized price, returns, RSI, MACD, volatility.
    /// </summary>
    public static float[] ComputeFeatures(List<TickData> window)
    {
        var prices = window.Select(t => (float)t.Price).ToArray();
        var features = new float[20];

        // Normalized price
        var min = prices.Min();
        var max = prices.Max();
        var range = max - min;
        if (range > 0)
        {
            for (int i = 0; i < prices.Length; i++)
                prices[i] = (prices[i] - min) / range;
        }

        // Copy normalized prices as first features
        for (int i = 0; i < Math.Min(10, prices.Length); i++)
            features[i] = prices[i];

        // Returns
        for (int i = 1; i < prices.Length; i++)
            features[10 + Math.Min(i - 1, 9)] = prices[i] - prices[i - 1];

        // RSI (simplified)
        features[19] = ComputeRSI(window.Select(t => t.Price).ToList());

        return features;
    }

    /// <summary>
    /// Computes RSI (Relative Strength Index) for a price series.
    /// </summary>
    public static float ComputeRSI(List<double> prices, int period = 14)
    {
        if (prices.Count < period + 1) return 50f;

        var gains = 0.0;
        var losses = 0.0;

        for (int i = prices.Count - period; i < prices.Count; i++)
        {
            var change = prices[i] - prices[i - 1];
            if (change > 0) gains += change;
            else losses -= change;
        }

        var avgGain = gains / period;
        var avgLoss = losses / period;

        if (avgLoss == 0) return 100f;
        var rs = avgGain / avgLoss;
        return (float)(100 - (100 / (1 + rs)));
    }

    /// <summary>
    /// Normalizes features to zero mean, unit variance.
    /// </summary>
    public static List<DriftSample> Normalize(List<DriftSample> samples)
    {
        if (samples.Count == 0) return samples;

        var featureCount = samples[0].Features.Length;
        var means = new float[featureCount];
        var stds = new float[featureCount];

        foreach (var s in samples)
            for (int i = 0; i < featureCount; i++)
                means[i] += s.Features[i];

        for (int i = 0; i < featureCount; i++)
            means[i] /= samples.Count;

        foreach (var s in samples)
            for (int i = 0; i < featureCount; i++)
                stds[i] += (s.Features[i] - means[i]) * (s.Features[i] - means[i]);

        for (int i = 0; i < featureCount; i++)
        {
            stds[i] = MathF.Sqrt(stds[i] / samples.Count);
            if (stds[i] == 0) stds[i] = 1;
        }

        foreach (var s in samples)
            for (int i = 0; i < featureCount; i++)
                s.Features[i] = (s.Features[i] - means[i]) / stds[i];

        return samples;
    }

    /// <summary>
    /// Splits data into train/validation/test (70/15/15).
    /// </summary>
    public static (List<DriftSample> train, List<DriftSample> val, List<DriftSample> test) Split(
        List<DriftSample> samples, double trainRatio = 0.7, double valRatio = 0.15)
    {
        var trainCount = (int)(samples.Count * trainRatio);
        var valCount = (int)(samples.Count * valRatio);

        var train = samples.Take(trainCount).ToList();
        var val = samples.Skip(trainCount).Take(valCount).ToList();
        var test = samples.Skip(trainCount + valCount).ToList();

        return (train, val, test);
    }
}

public sealed class DriftSample
{
    public float[] Features { get; set; } = Array.Empty<float>();
    public float Direction { get; set; }
    public float Magnitude { get; set; }
    public DateTime Timestamp { get; set; }
}
