using AI_Brain.NeuralNetwork.Interfaces;

namespace AI_Brain.NeuralNetwork
{
    public class Synapse : ISynapse
    {
        public INeuron Source { get; }
        public INeuron Target { get; }
        public double Weight { get; set; }
        public double WeightDelta { get; set; }

        public Synapse(INeuron source, INeuron target, double weight)
        {
            Source = source;
            Target = target;
            Weight = weight;
        }
    }
}
