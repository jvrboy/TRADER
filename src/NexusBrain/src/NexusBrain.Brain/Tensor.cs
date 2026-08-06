namespace NexusBrain.Brain;

/// <summary>Activation functions used by the neural network.</summary>
public enum Activation
{
    Sigmoid,
    Tanh,
    Relu,
    LeakyRelu,
    Swish
}

public static class Activations
{
    public static double Fn(Activation a, double x)
        => a switch
        {
            Activation.Sigmoid => 1.0 / (1.0 + Math.Exp(-x)),
            Activation.Tanh => Math.Tanh(x),
            Activation.Relu => x > 0 ? x : 0,
            Activation.LeakyRelu => x > 0 ? x : 0.01 * x,
            Activation.Swish => x / (1.0 + Math.Exp(-x)),
            _ => x
        };

    public static double Derivative(Activation a, double x, double y)
        => a switch
        {
            Activation.Sigmoid => y * (1 - y),
            Activation.Tanh => 1 - y * y,
            Activation.Relu => x > 0 ? 1 : 0,
            Activation.LeakyRelu => x > 0 ? 1 : 0.01,
            Activation.Swish => y + (1 - y) / (1 + Math.Exp(-x)),
            _ => 1
        };
}

/// <summary>Simple dense tensor used for forward/backward passes.</summary>
public sealed class Tensor
{
    public double[] Data { get; }
    public int Rows { get; }
    public int Cols { get; }

    public Tensor(int rows, int cols)
    {
        Rows = rows; Cols = cols;
        Data = new double[rows * cols];
    }

    public double this[int r, int c]
    {
        get => Data[r * Cols + c];
        set => Data[r * Cols + c] = value;
    }

    public void FillRandom(Random rng, double scale = 0.1)
    {
        for (int i = 0; i < Data.Length; i++)
            Data[i] = (rng.NextDouble() * 2 - 1) * scale;
    }

    public double[] ToVector() => (double[])Data.Clone();
}
