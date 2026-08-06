using Brain.Training;
using Brain.Core.Ensemble;
using Xunit;

namespace Brain.Tests;

public class TrainingTests
{
    [Fact]
    public void DerivApiClient_GenerateSyntheticData_ProducesValidTicks()
    {
        var ticks = DerivApiClient.GenerateSyntheticData("R_10", 100);

        Assert.Equal(100, ticks.Count);
        Assert.All(ticks, t => Assert.True(t.Price > 0));
        Assert.All(ticks, t => Assert.Equal("R_10", t.Symbol));
    }

    [Fact]
    public void DriftSwitchDataPreparer_Prepare_ProducesValidSamples()
    {
        var ticks = DerivApiClient.GenerateSyntheticData("R_10", 200);
        var samples = DriftSwitchDataPreparer.Prepare(ticks, 10);

        Assert.True(samples.Count > 0);
        Assert.All(samples, s => Assert.Equal(20, s.Features.Length));
        Assert.All(samples, s => Assert.True(s.Direction == 0f || s.Direction == 1f));
    }

    [Fact]
    public void DriftSwitchDataPreparer_Split_RatiosAreCorrect()
    {
        var ticks = DerivApiClient.GenerateSyntheticData("R_10", 1000);
        var samples = DriftSwitchDataPreparer.Prepare(ticks, 10);
        var (train, val, test) = DriftSwitchDataPreparer.Split(samples);

        Assert.True(train.Count > val.Count);
        Assert.True(val.Count >= test.Count - 1);
        Assert.Equal(samples.Count, train.Count + val.Count + test.Count);
    }

    [Fact]
    public void DriftSwitchDataPreparer_ComputeRSI_ReturnsValidRange()
    {
        var prices = new List<double>();
        var rng = new Random(42);
        var price = 100.0;
        for (int i = 0; i < 50; i++)
        {
            price *= 1 + (rng.NextDouble() - 0.5) * 0.01;
            prices.Add(price);
        }

        var rsi = DriftSwitchDataPreparer.ComputeRSI(prices, 14);

        Assert.True(rsi >= 0 && rsi <= 100);
    }

    [Fact]
    public async Task EnsembleTrainer_TrainAsync_CompletesSuccessfully()
    {
        var ensemble = EnsembleBuilder.Build(20, 20, 2);
        var apiClient = new DerivApiClient();
        var trainer = new EnsembleTrainer(ensemble, apiClient);

        var result = await trainer.TrainAsync(new[] { 10 }, epochs: 2, learningRate: 0.01f);

        Assert.True(result.Success);
        Assert.Single(result.IndexResults);
        Assert.True(result.TrainingTimeMs > 0);
    }
}
