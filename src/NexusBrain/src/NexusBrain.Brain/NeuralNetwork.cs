namespace NexusBrain.Brain;

/// <summary>
/// A fully-connected multi-layer neural network with backpropagation, momentum,
/// and configurable activations. This is the "neurons" of the brain — each layer
/// is a bank of trainable neurons.
/// </summary>
public sealed class NeuralNetwork
{
    public int InputDim { get; }
    public int[] HiddenDims { get; }
    public int OutputDim { get; }

    private readonly Activation _hiddenAct;
    private readonly Activation _outputAct;
    private readonly List<Layer> _layers = new();
    private readonly Random _rng;
    private readonly double _momentum;

    private sealed class Layer
    {
        public double[,] W = null!;
        public double[,] VW = null!;      // momentum buffer
        public double[] B = null!;
        public double[] VB = null!;
        public double[] Z = null!;        // pre-activation
        public double[] A = null!;        // post-activation
        public double[] Delta = null!;
        public Activation Act;
    }

    public NeuralNetwork(int inputDim, int[] hiddenDims, int outputDim,
        Activation hiddenAct = Activation.Tanh,
        Activation outputAct = Activation.Sigmoid,
        double momentum = 0.9,
        int? seed = null)
    {
        InputDim = inputDim; HiddenDims = hiddenDims; OutputDim = outputDim;
        _hiddenAct = hiddenAct; _outputAct = outputAct; _momentum = momentum;
        _rng = seed is null ? new Random() : new Random(seed.Value);

        int prev = inputDim;
        foreach (var h in hiddenDims)
        {
            _layers.Add(MakeLayer(prev, h, hiddenAct));
            prev = h;
        }
        _layers.Add(MakeLayer(prev, outputDim, outputAct));
    }

    private Layer MakeLayer(int rows, int cols, Activation act)
    {
        var l = new Layer { Act = act };
        l.W = new double[rows, cols];
        l.VW = new double[rows, cols];
        l.B = new double[cols];
        l.VB = new double[cols];
        double scale = Math.Sqrt(2.0 / (rows + cols));
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                l.W[r, c] = (_rng.NextDouble() * 2 - 1) * scale;
        for (int c = 0; c < cols; c++) l.B[c] = (_rng.NextDouble() * 2 - 1) * 0.1;
        return l;
    }

    /// <summary>Forward pass; returns output activations.</summary>
    public double[] Forward(double[] input)
    {
        double[] prev = input;
        foreach (var l in _layers)
        {
            l.Z = new double[l.B.Length];
            l.A = new double[l.B.Length];
            for (int c = 0; c < l.B.Length; c++)
            {
                double z = l.B[c];
                for (int r = 0; r < prev.Length; r++) z += prev[r] * l.W[r, c];
                l.Z[c] = z;
                l.A[c] = Activations.Fn(l.Act, z);
            }
            prev = l.A;
        }
        return prev;
    }

    /// <summary>Backward pass + weight update for a supervised target.</summary>
    public double Backprop(double[] input, double[] target, double lr)
    {
        var output = Forward(input);
        var last = _layers[_layers.Count - 1];

        // Output layer deltas
        for (int c = 0; c < last.B.Length; c++)
        {
            double err = target[c] - last.A[c];
            last.Delta = last.Delta ?? new double[last.B.Length];
            last.Delta[c] = err * Activations.Derivative(last.Act, last.Z[c], last.A[c]);
        }

        // Hidden layer deltas (backward)
        for (int li = _layers.Count - 2; li >= 0; li--)
        {
            var l = _layers[li];
            var next = _layers[li + 1];
            l.Delta = new double[l.B.Length];
            for (int c = 0; c < l.B.Length; c++)
            {
                double sum = 0;
                for (int nc = 0; nc < next.B.Length; nc++)
                    sum += next.Delta[nc] * next.W[c, nc];
                l.Delta[c] = sum * Activations.Derivative(l.Act, l.Z[c], l.A[c]);
            }
        }

        // Update weights (with momentum)
        double totalErr = 0;
        double[] prevAct = input;
        for (int li = 0; li < _layers.Count; li++)
        {
            var l = _layers[li];
            for (int c = 0; c < l.B.Length; c++)
            {
                l.VB[c] = _momentum * l.VB[c] + lr * l.Delta[c];
                l.B[c] += l.VB[c];
                for (int r = 0; r < prevAct.Length; r++)
                {
                    l.VW[r, c] = _momentum * l.VW[r, c] + lr * l.Delta[c] * prevAct[r];
                    l.W[r, c] += l.VW[r, c];
                }
            }
            prevAct = l.A;
            totalErr += l.Delta.Sum(d => d * d);
        }
        return totalErr;
    }

    /// <summary>Train on a single (input, target) pair; returns squared error.</summary>
    public double Train(double[] input, double[] target, double lr)
        => Backprop(input, target, lr);

    /// <summary>Predict with a single output (regression / binary).</summary>
    public double PredictSingle(double[] input)
    {
        var o = Forward(input);
        return o.Length == 1 ? o[0] : o[0];
    }

    /// <summary>Predict with full output vector.</summary>
    public double[] Predict(double[] input) => Forward(input);

    /// <summary>Total number of trainable parameters.</summary>
    public int ParameterCount
    {
        get
        {
            int total = 0;
            foreach (var l in _layers) total += l.W.Length + l.B.Length;
            return total;
        }
    }

    /// <summary>Snapshot weights to a flat array (for persistence/transfer).</summary>
    public double[] GetWeights()
    {
        var list = new List<double>();
        foreach (var l in _layers)
        {
            for (int r = 0; r < l.W.GetLength(0); r++)
                for (int c = 0; c < l.W.GetLength(1); c++)
                    list.Add(l.W[r, c]);
            foreach (var b in l.B) list.Add(b);
        }
        return list.ToArray();
    }

    /// <summary>Restore weights from a flat array (must match <see cref="GetWeights"/> count).</summary>
    public void SetWeights(double[] w)
    {
        int idx = 0;
        foreach (var l in _layers)
        {
            for (int r = 0; r < l.W.GetLength(0); r++)
                for (int c = 0; c < l.W.GetLength(1); c++)
                    l.W[r, c] = w[idx++];
            for (int c = 0; c < l.B.Length; c++) l.B[c] = w[idx++];
        }
    }
}
