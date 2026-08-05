using System.Text.Json;

namespace DsiAgentic.Kernels;

/// <summary>
/// A self-contained neuron consisting of one KernelBank plus a linear output.
/// Trainable online without a full backprop framework — uses gradient-free nudges
/// suitable for streaming market data and small feature vectors (<= 64 dims).
/// </summary>
public sealed class Neuron
{
    public string Name { get; }
    public int Dim { get; }
    public KernelBank Bank { get; }
    public double W;
    public double B;
    public long UpdateCount;

    public Neuron(string name, int dim, IEnumerable<KernelType>? kernels = null)
    {
        Name = name; Dim = dim;
        kernels ??= new[] { KernelType.Rbf, KernelType.Sigmoid, KernelType.Tanh, KernelType.Swish, KernelType.Softplus };
        Bank = new KernelBank(name + ":bank", dim, kernels);
        W = 1.0; B = 0.0;
    }

    public double Predict(double[] x)
    {
        var m = Bank.Forward(x);
        var y = 1.0 / (1.0 + Math.Exp(-(W * (m - 0.5) * 4 + B)));
        return y;
    }

    public void Learn(double[] x, double target, double lr = 0.02)
    {
        Bank.Learn(x, target, lr);
        var y = Predict(x);
        double err = target - y;
        var m = Bank.Forward(x);
        W += lr * err * (m - 0.5) * 4;
        B += lr * err;
        UpdateCount++;
    }

    public string Serialize()
    {
        var kernels = new List<object>();
        foreach (var k in Bank.Kernels)
        {
            if (k is MicroKernel mk)
                kernels.Add(new { name = mk.Name, type = mk.Type.ToString(), weights = mk.Weights, center = mk.Center, bias = mk.Bias, gamma = mk.Gamma });
        }
        return JsonSerializer.Serialize(new
        {
            name = Name,
            dim = Dim,
            w = W,
            b = B,
            updates = UpdateCount,
            mix = Bank.Mix,
            kernels
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}
