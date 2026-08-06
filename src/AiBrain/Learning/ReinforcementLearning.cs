using AI_Brain.NeuralNetwork.Interfaces;

namespace AI_Brain.Learning
{
    public class ReinforcementLearning
    {
        private Backpropagation _backprop = new Backpropagation();

        public void Update(NeuralNetwork.NeuralNetwork network, double[] state, double[] action, double reward, double learningRate)
        {
            // Simple Q-learning inspired update: 
            // adjust the network to predict higher rewards for the taken action in the given state
            double[] targets = network.GetOutput(); // Current predictions
            
            // Adjust the target for the chosen action based on the reward
            // In a full Q-learning implementation, this would involve the max future reward
            for (int i = 0; i < action.Length && i < targets.Length; i++)
            {
                if (action[i] > 0.5) // If this action was "chosen"
                {
                    targets[i] += learningRate * (reward - targets[i]);
                }
            }

            _backprop.Train(network, state, targets, learningRate);
        }
    }
}
