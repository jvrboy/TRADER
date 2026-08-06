using Brain.Core.Networks;

namespace Brain.Core.Ensemble;

/// <summary>
/// Factory that creates the 1000+ network ensemble with the specified distribution:
/// 70% feed-forward, 20% recurrent (LSTM), 10% convolutional (1D).
/// </summary>
public static class EnsembleBuilder
{
    public static NeuralEnsemble Build(int totalNetworks = 1024, int inputSize = 20, int outputSize = 2)
    {
        var ensemble = new NeuralEnsemble();
        var rng = new Random(42);

        var numFeedForward = (int)(totalNetworks * 0.7);
        var numLstm = (int)(totalNetworks * 0.2);
        var numConv = totalNetworks - numFeedForward - numLstm;

        for (int i = 0; i < numFeedForward; i++)
        {
            var hiddenLayers = GenerateRandomHiddenLayers(rng);
            var activation = rng.Next(2) == 0 ? ActivationType.ReLU : ActivationType.Tanh;
            ensemble.AddNetwork(new FeedForwardNetwork(inputSize, hiddenLayers, outputSize, activation, rng));
        }

        for (int i = 0; i < numLstm; i++)
        {
            var hiddenSize = rng.Next(16, 64);
            ensemble.AddNetwork(new LSTMNetwork(inputSize, hiddenSize, outputSize, rng));
        }

        for (int i = 0; i < numConv; i++)
        {
            var kernelSize = rng.Next(3, 7);
            var numFilters = rng.Next(4, 16);
            ensemble.AddNetwork(new Convolutional1DNetwork(inputSize, kernelSize, numFilters, outputSize, rng: rng));
        }

        return ensemble;
    }

    private static int[] GenerateRandomHiddenLayers(Random rng)
    {
        var numLayers = rng.Next(2, 6);
        var layers = new int[numLayers];
        for (int i = 0; i < numLayers; i++)
            layers[i] = rng.Next(16, 128);
        return layers;
    }
}
