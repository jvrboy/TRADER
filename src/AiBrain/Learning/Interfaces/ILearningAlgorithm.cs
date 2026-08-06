using AI_Brain.NeuralNetwork;

namespace AI_Brain.Learning.Interfaces
{
    public interface ILearningAlgorithm
    {
        void Train(NeuralNetwork.NeuralNetwork network, double[] inputs, double[] targets, double learningRate);
    }
}
