namespace AI_Brain.NeuralNetwork.Interfaces
{
    public interface INeuralLayer
    {
        List<INeuron> Neurons { get; }
        void FeedForward();
    }
}
