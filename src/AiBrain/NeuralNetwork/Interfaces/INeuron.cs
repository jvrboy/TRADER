namespace AI_Brain.NeuralNetwork.Interfaces
{
    public interface INeuron
    {
        double Activation { get; set; }
        double Bias { get; set; }
        double Delta { get; set; }
        List<ISynapse> IncomingSynapses { get; }
        List<ISynapse> OutgoingSynapses { get; }
        void Activate();
    }
}
