using AI_Brain.NeuralNetwork.Interfaces;

namespace AI_Brain.NeuralNetwork
{
    public class NeuralLayer : INeuralLayer
    {
        public List<INeuron> Neurons { get; } = new List<INeuron>();

        public void FeedForward()
        {
            foreach (var neuron in Neurons)
            {
                neuron.Activate();
            }
        }
    }
}
