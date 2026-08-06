using Brain.Core.Math;

namespace Brain.Core.Networks;

/// <summary>
/// Base class for all neural networks in the ensemble.
/// Each network has a unique ID, weight set, and validation accuracy score.
/// </summary>
public abstract class NeuralNetworkBase
{
    public Guid Id { get; } = Guid.NewGuid();
    public NetworkType Type { get; }
    public float ValidationAccuracy { get; set; } = 0.5f;
    public int InputSize { get; protected set; }
    public int OutputSize { get; protected set; }

    protected NeuralNetworkBase(NetworkType type, int inputSize, int outputSize)
    {
        Type = type;
        InputSize = inputSize;
        OutputSize = outputSize;
    }

    /// <summary>
    /// Forward pass: produces output predictions from input features.
    /// </summary>
    public abstract float[] Forward(float[] input);

    /// <summary>
    /// Backward pass: computes gradients and updates weights.
    /// </summary>
    public abstract void Backward(float[] input, float[] target, float learningRate);

    /// <summary>
    /// Saves network weights to a binary stream.
    /// </summary>
    public abstract void Save(BinaryWriter writer);

    /// <summary>
    /// Loads network weights from a binary stream.
    /// </summary>
    public abstract void Load(BinaryReader reader);
}

public enum NetworkType
{
    FeedForward,
    LSTM,
    GRU,
    Convolutional1D
}
