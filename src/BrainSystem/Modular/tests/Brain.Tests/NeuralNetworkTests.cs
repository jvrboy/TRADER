using Brain.Core.Networks;
using Brain.Core.Ensemble;
using Xunit;

namespace Brain.Tests;

public class NeuralNetworkTests
{
    [Fact]
    public void FeedForward_Forward_ProducesCorrectOutputSize()
    {
        var network = new FeedForwardNetwork(10, new[] { 20, 15 }, 2);
        var input = new float[10];
        for (int i = 0; i < 10; i++) input[i] = 0.5f;

        var output = network.Forward(input);

        Assert.Equal(2, output.Length);
    }

    [Fact]
    public void FeedForward_Backward_UpdatesWeights()
    {
        var network = new FeedForwardNetwork(5, new[] { 10 }, 2);
        var input = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
        var target = new float[] { 1f, 0f };

        var outputBefore = network.Forward(input);
        network.Backward(input, target, 0.01f);
        var outputAfter = network.Forward(input);

        Assert.NotEqual(outputBefore, outputAfter);
    }

    [Fact]
    public void LSTM_Forward_ProducesCorrectOutputSize()
    {
        var network = new LSTMNetwork(10, 32, 2);
        var input = new float[10];
        for (int i = 0; i < 10; i++) input[i] = 0.5f;

        var output = network.Forward(input);

        Assert.Equal(2, output.Length);
    }

    [Fact]
    public void LSTM_ResetState_ClearsInternalState()
    {
        var network = new LSTMNetwork(10, 32, 2);
        var input = new float[10];
        for (int i = 0; i < 10; i++) input[i] = 0.5f;

        network.Forward(input);
        network.ResetState();

        var output = network.Forward(input);
        Assert.Equal(2, output.Length);
    }

    [Fact]
    public void Convolutional1D_Forward_ProducesCorrectOutputSize()
    {
        var network = new Convolutional1DNetwork(20, 5, 8, 2);
        var input = new float[20];
        for (int i = 0; i < 20; i++) input[i] = 0.5f;

        var output = network.Forward(input);

        Assert.Equal(2, output.Length);
    }

    [Fact]
    public void EnsembleBuilder_Build_CreatesCorrectNumberOfNetworks()
    {
        var ensemble = EnsembleBuilder.Build(100, 20, 2);

        Assert.Equal(100, ensemble.Count);
    }

    [Fact]
    public void Ensemble_Predict_ReturnsValidPrediction()
    {
        var ensemble = EnsembleBuilder.Build(50, 20, 2);
        var input = new float[20];
        for (int i = 0; i < 20; i++) input[i] = 0.5f;

        var prediction = ensemble.Predict(input, 10);

        Assert.Equal(10, prediction.DriftIndex);
        Assert.Equal(50, prediction.NetworkCount);
        Assert.True(prediction.Confidence >= 0 && prediction.Confidence <= 1);
    }
}
