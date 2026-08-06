using Brain.Core.Math;

namespace Brain.Core.Networks;

/// <summary>
/// Feed-forward neural network with configurable hidden layers and activation functions.
/// Uses SIMD-accelerated matrix multiplication for forward/backward passes.
/// </summary>
public sealed class FeedForwardNetwork : NeuralNetworkBase
{
    private readonly Matrix[] _weights;
    private readonly float[][] _biases;
    private readonly float[][] _activations;
    private readonly float[][] _preActivations;
    private readonly ActivationType _activation;

    public FeedForwardNetwork(int inputSize, int[] hiddenLayers, int outputSize,
        ActivationType activation = ActivationType.ReLU, Random? rng = null)
        : base(NetworkType.FeedForward, inputSize, outputSize)
    {
        _activation = activation;
        rng ??= new Random(Id.GetHashCode());

        var layerSizes = new[] { inputSize }.Concat(hiddenLayers).Concat(new[] { outputSize }).ToArray();
        var numLayers = layerSizes.Length - 1;

        _weights = new Matrix[numLayers];
        _biases = new float[numLayers][];
        _activations = new float[numLayers][];
        _preActivations = new float[numLayers][];

        for (int i = 0; i < numLayers; i++)
        {
            var scale = MathF.Sqrt(2f / layerSizes[i]);
            _weights[i] = new Matrix(layerSizes[i + 1], layerSizes[i]);
            _weights[i].InitializeRandom(rng, scale);
            _biases[i] = new float[layerSizes[i + 1]];
            _activations[i] = new float[layerSizes[i + 1]];
            _preActivations[i] = new float[layerSizes[i + 1]];
        }
    }

    public override float[] Forward(float[] input)
    {
        var current = input.AsSpan();
        for (int layer = 0; layer < _weights.Length; layer++)
        {
            _weights[layer].Multiply(current, _preActivations[layer]);
            for (int j = 0; j < _biases[layer].Length; j++)
                _preActivations[layer][j] += _biases[layer][j];

            var actType = layer == _weights.Length - 1 ? ActivationType.Linear : _activation;
            _preActivations[layer].AsSpan().CopyTo(_activations[layer]);
            ActivationFunctions.Apply(_activations[layer], actType);
            current = _activations[layer].AsSpan();
        }
        return _activations[^1].ToArray();
    }

    public override void Backward(float[] input, float[] target, float learningRate)
    {
        Forward(input);

        var numLayers = _weights.Length;
        var deltas = new float[numLayers][];

        var outputLayer = numLayers - 1;
        deltas[outputLayer] = new float[OutputSize];
        for (int i = 0; i < OutputSize; i++)
        {
            var error = _activations[outputLayer][i] - target[i];
            deltas[outputLayer][i] = error;
        }

        for (int layer = outputLayer - 1; layer >= 0; layer--)
        {
            deltas[layer] = new float[_weights[layer].Rows];
            for (int i = 0; i < _weights[layer].Rows; i++)
            {
                var sum = 0f;
                for (int j = 0; j < _weights[layer + 1].Rows; j++)
                    sum += deltas[layer + 1][j] * _weights[layer + 1][j, i];
                var deriv = _activation == ActivationType.ReLU
                    ? ActivationFunctions.ReLUDerivative(_preActivations[layer][i])
                    : ActivationFunctions.TanhDerivative(_preActivations[layer][i]);
                deltas[layer][i] = sum * deriv;
            }
        }

        for (int layer = 0; layer < numLayers; layer++)
        {
            var prevActivations = layer == 0 ? input : _activations[layer - 1];
            for (int i = 0; i < _weights[layer].Rows; i++)
            {
                for (int j = 0; j < _weights[layer].Cols; j++)
                    _weights[layer][i, j] -= learningRate * deltas[layer][i] * prevActivations[j];
                _biases[layer][i] -= learningRate * deltas[layer][i];
            }
        }
    }

    public override void Save(BinaryWriter writer)
    {
        writer.Write((int)Type);
        writer.Write(InputSize);
        writer.Write(OutputSize);
        writer.Write((int)_activation);
        writer.Write(_weights.Length);
        foreach (var w in _weights)
            w.Save(writer);
        foreach (var b in _biases)
        {
            writer.Write(b.Length);
            foreach (var v in b) writer.Write(v);
        }
    }

    public override void Load(BinaryReader reader)
    {
        reader.ReadInt32();
        reader.ReadInt32();
        reader.ReadInt32();
        _ = (ActivationType)reader.ReadInt32();
        var numLayers = reader.ReadInt32();
        for (int i = 0; i < numLayers; i++)
            _ = Matrix.Load(reader);
    }
}
