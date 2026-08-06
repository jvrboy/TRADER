using AI_Brain.NeuralNetwork.Interfaces;

namespace AI_Brain.NeuralNetwork
{
    public class NeuralNetwork
    {
        public List<INeuralLayer> Layers { get; } = new List<INeuralLayer>();

        public void FeedForward(double[] inputs)
        {
            if (Layers.Count == 0) return;

            var inputLayer = Layers[0];
            for (int i = 0; i < inputs.Length && i < inputLayer.Neurons.Count; i++)
            {
                inputLayer.Neurons[i].Activation = inputs[i];
            }

            for (int i = 1; i < Layers.Count; i++)
            {
                Layers[i].FeedForward();
            }
        }

        public double[] GetOutput()
        {
            if (Layers.Count == 0) return Array.Empty<double>();
            return Layers.Last().Neurons.Select(n => n.Activation).ToArray();
        }

        public static NeuralNetwork Create(int[] topology)
        {
            var network = new NeuralNetwork();
            var rand = new Random();

            for (int i = 0; i < topology.Length; i++)
            {
                var layer = new NeuralLayer();
                for (int j = 0; j < topology[i]; j++)
                {
                    layer.Neurons.Add(new Neuron(rand.NextDouble() * 2 - 1));
                }
                network.Layers.Add(layer);
            }

            // Connect layers
            for (int i = 0; i < network.Layers.Count - 1; i++)
            {
                var currentLayer = network.Layers[i];
                var nextLayer = network.Layers[i + 1];

                foreach (var source in currentLayer.Neurons)
                {
                    foreach (var target in nextLayer.Neurons)
                    {
                        var weight = rand.NextDouble() * 2 - 1;
                        var synapse = new Synapse(source, target, weight);
                        source.OutgoingSynapses.Add(synapse);
                        target.IncomingSynapses.Add(synapse);
                    }
                }
            }

            return network;
        }
    }
}
