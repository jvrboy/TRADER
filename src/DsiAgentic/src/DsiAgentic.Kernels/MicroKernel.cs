using DsiAgentic.Core;

namespace DsiAgentic.Kernels;

/// <summary>
/// Micro-kernel: a tiny stateless activation unit that takes a feature vector,
/// produces a scalar in [0,1] via a chosen kernel function (RBF, Sigmoid, ReLU,
/// Tanh, Linear). Kernels compose into KernelBanks that feed into Neurons.
/// </summary>
public enum KernelType { Rbf, Sigmoid, Tanh, ReLU, Linear, Softplus, Swish, Gauss }

public interface IMicroKernel
{
    string Name { get; }
    double Activate(double[] x);
}

public sealed class MicroKernel : IMicroKernel
{
    public string Name { get; }
    public KernelType Type { get; }
    public double[] Weights;
    public double[] Center;
    public double Bias;
    public double Gamma;

    public MicroKernel(string name, KernelType type, int dim, Random? rng = null)
    {
        Name = name; Type = type;
        rng ??= new Random(name.GetHashCode() & 0x7fffffff);
        Weights = new double[dim];
        Center = new double[dim];
        for (int i = 0; i < dim; i++)
        {
            Weights[i] = (rng.NextDouble() - 0.5) * 0.4;
            Center[i] = rng.NextDouble();
        }
        Bias = (rng.NextDouble() - 0.5) * 0.2;
        Gamma = 1.0;
    }

    public double Activate(double[] x)
    {
        switch (Type)
        {
            case KernelType.Rbf:
            {
                double s = 0;
                for (int i = 0; i < x.Length; i++) { var d = x[i] - Center[i]; s += d * d; }
                return Math.Exp(-Gamma * s);
            }
            case KernelType.Gauss:
            {
                double s = 0;
                for (int i = 0; i < x.Length; i++) { var d = x[i] - Center[i]; s += d * d; }
                return Math.Exp(-s / (2.0 * Math.Max(1e-6, Gamma)));
            }
            case KernelType.Sigmoid:
            {
                double s = Bias;
                for (int i = 0; i < x.Length; i++) s += Weights[i] * x[i];
                return 1.0 / (1.0 + Math.Exp(-s));
            }
            case KernelType.Tanh:
            {
                double s = Bias;
                for (int i = 0; i < x.Length; i++) s += Weights[i] * x[i];
                return 0.5 * (Math.Tanh(s) + 1.0);
            }
            case KernelType.ReLU:
            {
                double s = Bias;
                for (int i = 0; i < x.Length; i++) s += Weights[i] * x[i];
                var v = Math.Max(0, s);
                return 1.0 - Math.Exp(-v);
            }
            case KernelType.Softplus:
            {
                double s = Bias;
                for (int i = 0; i < x.Length; i++) s += Weights[i] * x[i];
                var sp = Math.Log(1 + Math.Exp(Math.Min(30, s)));
                return 1.0 - Math.Exp(-sp);
            }
            case KernelType.Swish:
            {
                double s = Bias;
                for (int i = 0; i < x.Length; i++) s += Weights[i] * x[i];
                var sig = 1.0 / (1.0 + Math.Exp(-s));
                var v = s * sig;
                return 1.0 / (1.0 + Math.Exp(-v));
            }
            default:
            {
                double s = Bias;
                for (int i = 0; i < x.Length; i++) s += Weights[i] * x[i];
                return Math.Max(0, Math.Min(1, 0.5 + 0.5 * s));
            }
        }
    }

    public void Nudge(double[] x, double target, double lr)
    {
        // stochastic gradient-free nudge for kernel weights
        double y = Activate(x);
        double err = target - y;
        for (int i = 0; i < Weights.Length; i++)
        {
            Weights[i] += lr * err * x[i];
            Center[i] += lr * err * (x[i] - Center[i]);
        }
        Bias += lr * err;
    }
}

public sealed class KernelBank
{
    public string Name { get; }
    public List<IMicroKernel> Kernels { get; } = new();
    public double[] Mix;

    public KernelBank(string name, int dim, IEnumerable<KernelType> kernelTypes)
    {
        Name = name;
        int idx = 0;
        foreach (var kt in kernelTypes)
            Kernels.Add(new MicroKernel($"{name}#{idx++}:{kt}", kt, dim));
        Mix = new double[Kernels.Count];
        for (int i = 0; i < Mix.Length; i++) Mix[i] = 1.0 / Mix.Length;
    }

    public double Forward(double[] x)
    {
        double acc = 0;
        for (int i = 0; i < Kernels.Count; i++) acc += Mix[i] * Kernels[i].Activate(x);
        return acc;
    }

    public void Learn(double[] x, double target, double lr = 0.01)
    {
        foreach (var k in Kernels) if (k is MicroKernel mk) mk.Nudge(x, target, lr);
        // adjust mixture toward the best-performing kernel
        double y = Forward(x); double err = target - y;
        for (int i = 0; i < Kernels.Count; i++)
        {
            var yi = Kernels[i].Activate(x);
            double contrib = 1.0 - Math.Abs(target - yi);
            Mix[i] = 0.98 * Mix[i] + 0.02 * contrib;
        }
        double sum = Mix.Sum(); if (sum > 0) for (int i = 0; i < Mix.Length; i++) Mix[i] /= sum;
    }
}
