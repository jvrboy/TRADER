using System.Text.Json;
using DsiAgentic.Kernels;

namespace DsiAgentic.Brains;

/// <summary>
/// Per-instrument brain ensemble. Six sub-brains cooperate:
///   Outcome   — predicts P(TP) from feature vector
///   Trend     — predicts continuation probability
///   Reversal  — predicts reversal probability
///   Volatility — predicts high-volatility probability
///   Regime    — predicts regime change probability
///   Meta      — blends the previous five into a final probability
/// Each sub-brain is a Neuron with its own KernelBank of micro-kernels.
/// </summary>
public sealed class BrainEnsemble
{
    public string Instrument { get; }
    public int Dim { get; }
    public Neuron Outcome { get; }
    public Neuron Trend { get; }
    public Neuron Reversal { get; }
    public Neuron Volatility { get; }
    public Neuron Regime { get; }
    public Neuron Meta { get; }
    public long TotalUpdates;

    public BrainEnsemble(string instrument, int dim)
    {
        Instrument = instrument; Dim = dim;
        Outcome = new Neuron($"{instrument}.outcome", dim, new[] { KernelType.Rbf, KernelType.Sigmoid, KernelType.Swish, KernelType.Softplus });
        Trend = new Neuron($"{instrument}.trend", dim, new[] { KernelType.Tanh, KernelType.Linear, KernelType.Swish });
        Reversal = new Neuron($"{instrument}.reversal", dim, new[] { KernelType.Rbf, KernelType.Gauss, KernelType.Tanh });
        Volatility = new Neuron($"{instrument}.volatility", dim, new[] { KernelType.ReLU, KernelType.Softplus, KernelType.Sigmoid });
        Regime = new Neuron($"{instrument}.regime", dim, new[] { KernelType.Rbf, KernelType.Swish, KernelType.Tanh });
        Meta = new Neuron($"{instrument}.meta", 5, new[] { KernelType.Sigmoid, KernelType.Tanh, KernelType.Swish, KernelType.Softplus });
    }

    public double PredictWinProbability(double[] x)
    {
        var yO = Outcome.Predict(x);
        var yT = Trend.Predict(x);
        var yR = Reversal.Predict(x);
        var yV = Volatility.Predict(x);
        var yG = Regime.Predict(x);
        var metaX = new[] { yO, yT, yR, yV, yG };
        return Meta.Predict(metaX);
    }

    public (double outcome, double trend, double reversal, double vol, double regime, double meta) DetailedPredict(double[] x)
    {
        var yO = Outcome.Predict(x); var yT = Trend.Predict(x); var yR = Reversal.Predict(x);
        var yV = Volatility.Predict(x); var yG = Regime.Predict(x);
        var m = Meta.Predict(new[] { yO, yT, yR, yV, yG });
        return (yO, yT, yR, yV, yG, m);
    }

    public void LearnFromOutcome(double[] x, double outcomeTarget,
        double trendTarget, double reversalTarget, double volTarget, double regimeTarget,
        double lr = 0.03)
    {
        Outcome.Learn(x, outcomeTarget, lr);
        Trend.Learn(x, trendTarget, lr);
        Reversal.Learn(x, reversalTarget, lr);
        Volatility.Learn(x, volTarget, lr);
        Regime.Learn(x, regimeTarget, lr);
        var metaX = new[]
        {
            Outcome.Predict(x), Trend.Predict(x), Reversal.Predict(x),
            Volatility.Predict(x), Regime.Predict(x)
        };
        Meta.Learn(metaX, outcomeTarget, lr);
        TotalUpdates++;
    }

    public void Save(string dir)
    {
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"{Instrument}.outcome.json"), Outcome.Serialize());
        File.WriteAllText(Path.Combine(dir, $"{Instrument}.trend.json"), Trend.Serialize());
        File.WriteAllText(Path.Combine(dir, $"{Instrument}.reversal.json"), Reversal.Serialize());
        File.WriteAllText(Path.Combine(dir, $"{Instrument}.volatility.json"), Volatility.Serialize());
        File.WriteAllText(Path.Combine(dir, $"{Instrument}.regime.json"), Regime.Serialize());
        File.WriteAllText(Path.Combine(dir, $"{Instrument}.meta.json"), Meta.Serialize());
        File.WriteAllText(Path.Combine(dir, $"{Instrument}.stats.json"),
            JsonSerializer.Serialize(new { instrument = Instrument, dim = Dim, updates = TotalUpdates },
                new JsonSerializerOptions { WriteIndented = true }));
    }
}
