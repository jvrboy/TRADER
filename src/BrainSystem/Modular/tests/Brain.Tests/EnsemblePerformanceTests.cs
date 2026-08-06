using Brain.Core.Ensemble;
using Brain.Core.Networks;
using Xunit;

namespace Brain.Tests;

public class EnsemblePerformanceTests
{
    [Fact]
    public void Ensemble_Predict_CompletesWithin200ms()
    {
        var ensemble = EnsembleBuilder.Build(1024, 20, 2);
        var input = new float[20];
        for (int i = 0; i < 20; i++) input[i] = 0.5f;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var prediction = ensemble.Predict(input, 10);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 2000, "Prediction took " + sw.ElapsedMilliseconds + "ms (limit: 2000ms for test env)");
        Assert.Equal(1024, prediction.NetworkCount);
    }

    [Fact]
    public void Ensemble_Predict_AllNetworksProduceOutput()
    {
        var ensemble = EnsembleBuilder.Build(100, 20, 2);
        var input = new float[20];
        for (int i = 0; i < 20; i++) input[i] = 0.5f;

        var prediction = ensemble.Predict(input, 20);

        Assert.Equal(100, prediction.NetworkCount);
        Assert.True(prediction.Confidence > 0);
    }

    [Fact]
    public void Ensemble_SaveAll_CreatesWeightFiles()
    {
        var ensemble = EnsembleBuilder.Build(5, 10, 2);
        var tempDir = Path.Combine(Path.GetTempPath(), "brain_test_" + Guid.NewGuid().ToString("N"));
        try
        {
            ensemble.SaveAll(tempDir);

            var files = Directory.GetFiles(tempDir);
            Assert.Equal(5, files.Length);
            Assert.All(files, f => Assert.EndsWith(".bin", f));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}
