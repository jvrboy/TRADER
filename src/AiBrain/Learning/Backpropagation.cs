using AI_Brain.Learning.Interfaces;
using AI_Brain.NeuralNetwork.Interfaces;

namespace AI_Brain.Learning
{
    public class Backpropagation : ILearningAlgorithm
    {
        public void Train(NeuralNetwork.NeuralNetwork network, double[] inputs, double[] targets, double learningRate)
        {
            network.FeedForward(inputs);

            // Calculate output layer deltas
            var outputLayer = network.Layers.Last();
            for (int i = 0; i < outputLayer.Neurons.Count; i++)
            {
                var neuron = outputLayer.Neurons[i];
                double error = targets[i] - neuron.Activation;
                neuron.Delta = error * SigmoidDerivative(neuron.Activation);
            }

            // Backpropagate deltas to hidden layers
            for (int i = network.Layers.Count - 2; i >= 1; i--)
            {
                var layer = network.Layers[i];
                foreach (var neuron in layer.Neurons)
                {
                    double error = 0;
                    foreach (var synapse in neuron.OutgoingSynapses)
                    {
                        error += synapse.Target.Delta * synapse.Weight;
                    }
                    neuron.Delta = error * SigmoidDerivative(neuron.Activation);
                }
            }

            // Update weights and biases
            for (int i = 1; i < network.Layers.Count; i++)
            {
                foreach (var neuron in network.Layers[i].Neurons)
                {
                    neuron.Bias += learningRate * neuron.Delta;
                    foreach (var synapse in neuron.IncomingSynapses)
                    {
                        synapse.WeightDelta = learningRate * neuron.Delta * synapse.Source.Activation;
                        synapse.Weight += synapse.WeightDelta;
                    }
                }
            }
        }

        private double SigmoidDerivative(double x)
        {
            return x * (1.0 - x);
        }
    }
}
