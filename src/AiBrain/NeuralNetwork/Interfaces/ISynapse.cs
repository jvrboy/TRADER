namespace AI_Brain.NeuralNetwork.Interfaces
{
    public interface ISynapse
    {
        INeuron Source { get; }
        INeuron Target { get; }
        double Weight { get; set; }
        double WeightDelta { get; set; }
    }
}
