using AI_Brain.NeuralNetwork.Interfaces;

namespace AI_Brain.NeuralNetwork
{
    public class Neuron : INeuron
    {
        public double Activation { get; set; }
        public double Bias { get; set; }
        public double Delta { get; set; }
        public List<ISynapse> IncomingSynapses { get; } = new List<ISynapse>();
        public List<ISynapse> OutgoingSynapses { get; } = new List<ISynapse>();

        public Neuron(double bias = 0)
        {
            Bias = bias;
        }

        public void Activate()
        {
            double sum = Bias;
            foreach (var synapse in IncomingSynapses)
            {
                sum += synapse.Source.Activation * synapse.Weight;
            }
            Activation = Sigmoid(sum);
        }

        private double Sigmoid(double x)
        {
            return 1.0 / (1.0 + Math.Exp(-x));
        }
    }
}
