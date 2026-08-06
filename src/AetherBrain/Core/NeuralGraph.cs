namespace AetherBrain.Core;

public sealed class NeuralGraph
{
    private readonly List<Neuron> _neurons = [];

    public IReadOnlyList<Neuron> Neurons => _neurons;

    public Neuron Add(string label, double bias = 0)
    {
        var neuron = new Neuron(label, bias);
        _neurons.Add(neuron);
        return neuron;
    }

    public void ConnectDense(double initialWeight = 0.08)
    {
        foreach (var source in _neurons)
        {
            foreach (var target in _neurons.Where(target => target.Id != source.Id))
            {
                var polarity = ((source.Id.GetHashCode() ^ target.Id.GetHashCode()) & 1) == 0 ? 1 : -1;
                source.Connect(target, initialWeight * polarity);
            }
        }
    }

    public IReadOnlyDictionary<Guid, double> Pulse(IReadOnlyList<double> inputs, int cycles = 3)
    {
        var state = _neurons
            .Select((neuron, index) => new { neuron.Id, Signal = index < inputs.Count ? inputs[index] : 0d })
            .ToDictionary(item => item.Id, item => item.Signal);

        for (var cycle = 0; cycle < Math.Max(1, cycles); cycle++)
        {
            state = _neurons.ToDictionary(neuron => neuron.Id, neuron => neuron.Activate(state));
        }

        return state;
    }
}
