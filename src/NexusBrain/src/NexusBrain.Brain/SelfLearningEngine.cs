namespace NexusBrain.Brain;

/// <summary>A single experience stored for replay learning.</summary>
public sealed record Experience(double[] State, double Action, double Reward, double[]? NextState);

/// <summary>
/// Self-learning engine that trains the neural brain online using reinforcement
/// learning: experiences are collected, scored by reward, replayed, and the
/// learning rate adapts based on recent performance (meta-learning).
/// </summary>
public sealed class SelfLearningEngine
{
    private readonly NeuralNetwork _net;
    private readonly List<Experience> _replayBuffer = new();
    private readonly int _maxBuffer;
    private readonly Random _rng;
    private double _lr;
    private double _bestLoss = double.MaxValue;
    private int _patience;
    private double _emaLoss = 1.0;

    public int TrainSteps { get; private set; }
    public double CumulativeReward { get; private set; }
    public double LastLoss { get; private set; }
    public double LearningRate => _lr;

    public SelfLearningEngine(NeuralNetwork net, double initialLr = 0.02, int maxBuffer = 10000, int? seed = null)
    {
        _net = net;
        _lr = initialLr;
        _maxBuffer = maxBuffer;
        _rng = seed is null ? new Random() : new Random(seed.Value);
    }

    /// <summary>Record an experience for future replay.</summary>
    public void Record(Experience exp)
    {
        CumulativeReward += exp.Reward;
        lock (_replayBuffer)
        {
            _replayBuffer.Add(exp);
            if (_replayBuffer.Count > _maxBuffer)
                _replayBuffer.RemoveAt(0);
        }
    }

    /// <summary>Immediate supervised training step (e.g. from a labelled sample).</summary>
    public double Supervise(double[] state, double target, double lr)
    {
        var err = _net.Train(state, new[] { target }, lr);
        TrainSteps++;
        UpdateMeta(err);
        return err;
    }

    /// <summary>Reinforcement learning step: nudge toward a target action scaled by reward.</summary>
    public double Reinforce(double[] state, double target, double reward)
    {
        // Reward-weighted target: pull prediction toward target when reward is positive,
        // away (or neutral) when negative.
        double adjusted = target * Math.Tanh(Math.Max(0, reward)) + _net.PredictSingle(state) * (1 - Math.Abs(Math.Tanh(reward)));
        var err = _net.Train(state, new[] { adjusted }, _lr);
        TrainSteps++;
        UpdateMeta(err);
        return err;
    }

    /// <summary>Sample a mini-batch from the replay buffer and train on it.</summary>
    public double Replay(int batchSize = 32)
    {
        lock (_replayBuffer)
        {
            if (_replayBuffer.Count == 0) return 0;
            int n = Math.Min(batchSize, _replayBuffer.Count);
            double total = 0;
            for (int i = 0; i < n; i++)
            {
                var exp = _replayBuffer[_rng.Next(_replayBuffer.Count)];
                // TD-style target: current reward + discounted next prediction
                double nextPred = exp.NextState is null ? 0 : _net.PredictSingle(exp.NextState);
                double target = exp.Reward + 0.9 * nextPred;
                target = Math.Clamp(target, -1, 1);
                total += _net.Train(exp.State, new[] { target }, _lr);
                TrainSteps++;
            }
            var avg = total / n;
            UpdateMeta(avg);
            return avg;
        }
    }

    /// <summary>Meta-learning: adapt the learning rate and track best loss.</summary>
    private void UpdateMeta(double err)
    {
        LastLoss = err;
        _emaLoss = 0.95 * _emaLoss + 0.05 * err;
        if (err < _bestLoss)
        {
            _bestLoss = err;
            _patience = 0;
            _lr = Math.Min(_lr * 1.02, 0.1); // gently increase when improving
        }
        else
        {
            _patience++;
            if (_patience > 50)
            {
                _lr = Math.Max(_lr * 0.9, 0.001); // decay when plateau
                _patience = 0;
            }
        }
    }

    /// <summary>Exploration epsilon-greedy action selection (0..1).</summary>
    public double EpsilonGreedy(double[] state, double epsilon)
    {
        if (_rng.NextDouble() < epsilon)
            return _rng.NextDouble() * 2 - 1;
        return _net.PredictSingle(state);
    }
}
