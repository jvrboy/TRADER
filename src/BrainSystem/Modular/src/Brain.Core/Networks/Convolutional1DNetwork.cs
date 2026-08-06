using Brain.Core.Math;

namespace Brain.Core.Networks;

/// <summary>
/// 1D Convolutional neural network for pattern detection in market data.
/// Uses sliding window kernels followed by max-pooling and a dense output layer.
/// </summary>
public sealed class Convolutional1DNetwork : NeuralNetworkBase
{
    private readonly int _kernelSize;
    private readonly int _numFilters;
    private readonly float[][] _kernels;
    private readonly float[] _convBias;
    private readonly Matrix _denseWeights;
    private readonly float[] _denseBias;
    private readonly int _poolSize;

    public Convolutional1DNetwork(int inputSize, int kernelSize, int numFilters,
        int outputSize, int poolSize = 2, Random? rng = null)
        : base(NetworkType.Convolutional1D, inputSize, outputSize)
    {
        _kernelSize = kernelSize;
        _numFilters = numFilters;
        _poolSize = poolSize;
        rng ??= new Random(Id.GetHashCode());

        _kernels = new float[numFilters][];
        for (int i = 0; i < numFilters; i++)
        {
            _kernels[i] = new float[kernelSize];
            for (int j = 0; j < kernelSize; j++)
                _kernels[i][j] = (float)(rng.NextDouble() * 2 - 1) * MathF.Sqrt(2f / kernelSize);
        }
        _convBias = new float[numFilters];

        var convOutputSize = inputSize - kernelSize + 1;
        var pooledSize = convOutputSize / poolSize;
        var denseInputSize = numFilters * pooledSize;
        _denseWeights = new Matrix(outputSize, denseInputSize);
        _denseWeights.InitializeRandom(rng, MathF.Sqrt(2f / denseInputSize));
        _denseBias = new float[outputSize];
    }

    public override float[] Forward(float[] input)
    {
        var convOutputSize = InputSize - _kernelSize + 1;
        var convOutput = new float[_numFilters, convOutputSize];
        for (int f = 0; f < _numFilters; f++)
        {
            for (int i = 0; i < convOutputSize; i++)
            {
                var sum = _convBias[f];
                for (int k = 0; k < _kernelSize; k++)
                    sum += _kernels[f][k] * input[i + k];
                convOutput[f, i] = ActivationFunctions.ReLU(sum);
            }
        }

        var pooledSize = convOutputSize / _poolSize;
        var pooled = new float[_numFilters, pooledSize];
        for (int f = 0; f < _numFilters; f++)
        {
            for (int p = 0; p < pooledSize; p++)
            {
                var max = float.MinValue;
                for (int j = 0; j < _poolSize; j++)
                    max = MathF.Max(max, convOutput[f, p * _poolSize + j]);
                pooled[f, p] = max;
            }
        }

        var flattened = new float[_numFilters * pooledSize];
        for (int f = 0; f < _numFilters; f++)
            for (int p = 0; p < pooledSize; p++)
                flattened[f * pooledSize + p] = pooled[f, p];

        var output = new float[OutputSize];
        _denseWeights.Multiply(flattened, output);
        for (int i = 0; i < OutputSize; i++)
            output[i] += _denseBias[i];
        return output;
    }

    public override void Backward(float[] input, float[] target, float learningRate)
    {
        var output = Forward(input);
        var delta = new float[OutputSize];
        for (int i = 0; i < OutputSize; i++)
            delta[i] = output[i] - target[i];

        var convOutputSize = InputSize - _kernelSize + 1;
        var pooledSize = convOutputSize / _poolSize;
        var flattened = new float[_numFilters * pooledSize];

        for (int i = 0; i < _denseWeights.Rows; i++)
        {
            for (int j = 0; j < _denseWeights.Cols; j++)
                _denseWeights[i, j] -= learningRate * delta[i] * flattened[j];
            _denseBias[i] -= learningRate * delta[i];
        }
    }

    public override void Save(BinaryWriter writer)
    {
        writer.Write((int)Type);
        writer.Write(InputSize);
        writer.Write(OutputSize);
        writer.Write(_kernelSize);
        writer.Write(_numFilters);
        writer.Write(_poolSize);
        foreach (var kernel in _kernels)
        {
            writer.Write(kernel.Length);
            foreach (var v in kernel) writer.Write(v);
        }
        foreach (var v in _convBias) writer.Write(v);
        _denseWeights.Save(writer);
        foreach (var v in _denseBias) writer.Write(v);
    }

    public override void Load(BinaryReader reader)
    {
        reader.ReadInt32();
        reader.ReadInt32();
        reader.ReadInt32();
        reader.ReadInt32();
        reader.ReadInt32();
        reader.ReadInt32();
    }
}
