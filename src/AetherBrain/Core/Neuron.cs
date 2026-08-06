namespace AetherBrain.Core;

public sealed class Neuron
{
    private readonly Dictionary<Guid, double> _weights = [];

    public Neuron(string label, double bias = 0)
    {
        Id = Guid.NewGuid();
        Label = label;
        Bias = bias;
    }

    public Guid Id { get; }
    public string Label { get; }
    public double Bias { get; private set; }
    public IReadOnlyDictionary<Guid, double> Weights => _weights;

    public void Connect(Neuron target, double weight) => _weights[target.Id] = Math.Clamp(weight, -1, 1);

    public double Activate(IReadOnlyDictionary<Guid, double> signals)
    {
        var sum = Bias;
        foreach (var connection in _weights)
        {
            if (signals.TryGetValue(connection.Key, out var signal))
            {
                sum += signal * connection.Value;
            }
        }

        return Math.Tanh(sum);
    }

    public void AdaptBias(double delta, double learningRate) =>
        Bias = Math.Clamp(Bias + delta * learningRate, -2, 2);

    public void AdaptWeight(Guid targetId, double delta, double learningRate)
    {
        if (_weights.TryGetValue(targetId, out var weight))
        {
            _weights[targetId] = Math.Clamp(weight + delta * learningRate, -1, 1);
        }
    }
}
