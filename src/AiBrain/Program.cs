using AI_Brain.Core;

namespace AI_Brain
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Initializing ULTRA ADVANCED SELF-LEARNING AI BRAIN...");
            
            // Define a brain with 2 inputs, 4 hidden neurons, and 1 output
            var brain = new AgenticBrain(new int[] { 2, 4, 1 });
            var reflectionEngine = new SelfReflectionEngine();

            Console.WriteLine("Brain initialized. Starting learning loop...");

            // Simple task: Learn the XOR function
            double[][] trainingInputs = {
                new double[] { 0, 0 },
                new double[] { 0, 1 },
                new double[] { 1, 0 },
                new double[] { 1, 1 }
            };
            double[] expectedOutputs = { 0, 1, 1, 0 };

            for (int epoch = 0; epoch < 5000; epoch++)
            {
                double totalError = 0;
                for (int i = 0; i < trainingInputs.Length; i++)
                {
                    var input = trainingInputs[i];
                    var output = brain.Think(input);
                    
                    double error = Math.Abs(expectedOutputs[i] - output[0]);
                    totalError += error;

                    // Provide feedback (Reinforcement Learning)
                    double reward = 1.0 - error;
                    brain.LearnFromExperience(input, output, reward);
                }

                if (epoch % 1000 == 0)
                {
                    Console.WriteLine($"Epoch {epoch}: Average Error = {totalError / 4:F6}");
                    reflectionEngine.Reflect(brain);
                }
            }

            Console.WriteLine("\nTesting the brain's learned XOR logic:");
            foreach (var input in trainingInputs)
            {
                var result = brain.Think(input);
                Console.WriteLine($"Input: [{input[0]}, {input[1]}] -> Output: {result[0]:F4}");
            }

            Console.WriteLine("\nBrain simulation complete. All systems functional.");
        }
    }
}
