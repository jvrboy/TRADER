namespace NexusBrain.Core;

/// <summary>
/// Generates realistic synthetic training data for the Volatility Index and
/// Drift Switch Index. When live Deriv data is unavailable (offline), this lets
/// the brain train on statistically-similar regime-switching price paths.
/// </summary>
public static class TrainingDataGenerator
{
    /// <summary>
    /// Generate a synthetic candle series for a symbol with regime-switching
    /// (trending ↔ ranging) behavior, mimicking Deriv's VI/DSI indices.
    /// </summary>
    public static Candle[] GenerateCandles(string symbol, int count, int? seed = null, double? startPrice = null)
    {
        var rng = seed is null ? new Random() : new Random(seed.Value);
        var candles = new List<Candle>();

        double price = startPrice ?? 100.0;
        double drift = 0.0;          // current trend drift
        double vol = 0.004;          // current volatility
        double regime = 0.5;         // 0 = ranging, 1 = trending
        long epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - count * 60;

        for (int i = 0; i < count; i++)
        {
            // Regime switch (Drift Switch behavior) — occasionally flip
            if (rng.NextDouble() < 0.03) regime = rng.NextDouble();
            // Volatility regime (VI behavior) — occasionally spike/quiet
            if (rng.NextDouble() < 0.02) vol = rng.NextDouble() < 0.5 ? vol * (1 + rng.NextDouble()) : vol * (1 - rng.NextDouble() * 0.5);
            vol = Math.Clamp(vol, 0.001, 0.02);
            drift = regime > 0.6 ? (rng.NextDouble() - 0.35) * 0.01 : (rng.NextDouble() - 0.5) * 0.002;

            double open = price;
            double change = drift + (rng.NextDouble() * 2 - 1) * vol;
            double close = open * (1 + change);
            double wick = vol * rng.NextDouble();
            double high = Math.Max(open, close) * (1 + wick);
            double low = Math.Min(open, close) * (1 - wick * 0.5);
            double volume = 100 + rng.NextDouble() * 900;

            candles.Add(new Candle(epoch, open, high, low, close, volume));
            price = close;
            epoch += 60;
        }
        return candles.ToArray();
    }

}
