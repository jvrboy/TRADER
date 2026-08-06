using AI_Brain.NeuralNetwork.Interfaces;

namespace AI_Brain.Learning
{
    public class HebbianLearning
    {
        public void Apply(NeuralNetwork.NeuralNetwork network, double learningRate)
        {
            // Neurons that fire together, wire together
            foreach (var layer in network.Layers)
            {
                foreach (var neuron in layer.Neurons)
                {
                    foreach (var synapse in neuron.OutgoingSynapses)
                    {
                        // Strengthening connection if both neurons are active
                        double delta = learningRate * neuron.Activation * synapse.Target.Activation;
                        synapse.Weight += delta;
                    }
                }
            }
        }
    }
}
