using AI_Brain.NeuralNetwork;
using AI_Brain.Memory;
using AI_Brain.Learning;
using AI_Brain.Memory.Interfaces;

namespace AI_Brain.Core
{
    public class AgenticBrain
    {
        public NeuralNetwork.NeuralNetwork Network { get; private set; }
        public SensoryMemory Sensory { get; private set; }
        public WorkingMemory Working { get; private set; }
        public EpisodicMemory Episodic { get; private set; }
        public SemanticMemory Semantic { get; private set; }
        public ProceduralMemory Procedural { get; private set; }
        
        private Backpropagation _backprop = new Backpropagation();
        private ReinforcementLearning _rl = new ReinforcementLearning();

        public AgenticBrain(int[] topology)
        {
            Network = NeuralNetwork.NeuralNetwork.Create(topology);
            Sensory = new SensoryMemory();
            Working = new WorkingMemory();
            Episodic = new EpisodicMemory();
            Semantic = new SemanticMemory();
            Procedural = new ProceduralMemory();
        }

        public double[] Think(double[] inputs)
        {
            // 1. Perception: Store in sensory memory
            Sensory.Store(inputs);

            // 2. Attention: Process inputs through the network
            Network.FeedForward(inputs);
            double[] output = Network.GetOutput();

            // 3. Working Memory: Store current state and decision
            Working.Store(new { Input = inputs, Output = output });

            return output;
        }

        public void LearnFromExperience(double[] state, double[] action, double reward)
        {
            // 1. Episodic Memory: Store the experience
            var experience = new Experience
            {
                Timestamp = DateTime.Now,
                State = state,
                Action = action,
                Reward = reward
            };
            Episodic.Store(experience);

            // 2. Learning: Update the network based on reward
            _rl.Update(Network, state, action, reward, 0.1);

            // 3. Consolidation: Potential transfer from episodic to semantic memory
            if (reward > 0.8)
            {
                Semantic.Store(new Concept 
                { 
                    Name = $"Experience_{experience.Timestamp.Ticks}", 
                    Vector = state 
                });
            }
        }
    }
}
