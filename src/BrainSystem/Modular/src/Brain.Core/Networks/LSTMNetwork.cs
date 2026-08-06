using Brain.Core.Math;

namespace Brain.Core.Networks;

/// <summary>
/// LSTM recurrent neural network for time-series prediction.
/// Implements forget, input, and output gates with cell state.
/// </summary>
public sealed class LSTMNetwork : NeuralNetworkBase
{
    private readonly int _hiddenSize;
    private readonly Matrix _wf, _wi, _wc, _wo;
    private readonly Matrix _uf, _ui, _uc, _uo;
    private readonly float[] _bf, _bi, _bc, _bo;
    private float[] _hiddenState;
    private float[] _cellState;
    private readonly Matrix _outputWeights;
    private readonly float[] _outputBias;

    public LSTMNetwork(int inputSize, int hiddenSize, int outputSize, Random? rng = null)
        : base(NetworkType.LSTM, inputSize, outputSize)
    {
        _hiddenSize = hiddenSize;
        rng ??= new Random(Id.GetHashCode());

        var scale = MathF.Sqrt(2f / inputSize);
        _wf = new Matrix(hiddenSize, inputSize); _wf.InitializeRandom(rng, scale);
        _wi = new Matrix(hiddenSize, inputSize); _wi.InitializeRandom(rng, scale);
        _wc = new Matrix(hiddenSize, inputSize); _wc.InitializeRandom(rng, scale);
        _wo = new Matrix(hiddenSize, inputSize); _wo.InitializeRandom(rng, scale);

        var scaleU = MathF.Sqrt(2f / hiddenSize);
        _uf = new Matrix(hiddenSize, hiddenSize); _uf.InitializeRandom(rng, scaleU);
        _ui = new Matrix(hiddenSize, hiddenSize); _ui.InitializeRandom(rng, scaleU);
        _uc = new Matrix(hiddenSize, hiddenSize); _uc.InitializeRandom(rng, scaleU);
        _uo = new Matrix(hiddenSize, hiddenSize); _uo.InitializeRandom(rng, scaleU);

        _bf = new float[hiddenSize];
        _bi = new float[hiddenSize];
        _bc = new float[hiddenSize];
        _bo = new float[hiddenSize];

        _hiddenState = new float[hiddenSize];
        _cellState = new float[hiddenSize];

        _outputWeights = new Matrix(outputSize, hiddenSize);
        _outputWeights.InitializeRandom(rng, scaleU);
        _outputBias = new float[outputSize];
    }

    public override float[] Forward(float[] input)
    {
        var f = GateOutput(_wf, _uf, _bf, input, _hiddenState, ActivationType.Sigmoid);
        var i = GateOutput(_wi, _ui, _bi, input, _hiddenState, ActivationType.Sigmoid);
        var c = GateOutput(_wc, _uc, _bc, input, _hiddenState, ActivationType.Tanh);
        var o = GateOutput(_wo, _uo, _bo, input, _hiddenState, ActivationType.Sigmoid);

        for (int j = 0; j < _hiddenSize; j++)
            _cellState[j] = f[j] * _cellState[j] + i[j] * c[j];

        for (int j = 0; j < _hiddenSize; j++)
            _hiddenState[j] = o[j] * ActivationFunctions.Tanh(_cellState[j]);

        var output = new float[OutputSize];
        _outputWeights.Multiply(_hiddenState, output);
        for (int j = 0; j < OutputSize; j++)
            output[j] += _outputBias[j];
        return output;
    }

    private float[] GateOutput(Matrix w, Matrix u, float[] b, float[] input, float[] hidden, ActivationType act)
    {
        var result = new float[_hiddenSize];
        w.Multiply(input, result);
        var hiddenContrib = new float[_hiddenSize];
        u.Multiply(hidden, hiddenContrib);
        for (int j = 0; j < _hiddenSize; j++)
            result[j] += hiddenContrib[j] + b[j];
        ActivationFunctions.Apply(result, act);
        return result;
    }

    public override void Backward(float[] input, float[] target, float learningRate)
    {
        var output = Forward(input);
        var outputDelta = new float[OutputSize];
        for (int j = 0; j < OutputSize; j++)
            outputDelta[j] = output[j] - target[j];

        for (int i = 0; i < _outputWeights.Rows; i++)
        {
            for (int j = 0; j < _outputWeights.Cols; j++)
                _outputWeights[i, j] -= learningRate * outputDelta[i] * _hiddenState[j];
            _outputBias[i] -= learningRate * outputDelta[i];
        }
    }

    public void ResetState()
    {
        Array.Clear(_hiddenState, 0, _hiddenSize);
        Array.Clear(_cellState, 0, _hiddenSize);
    }

    public override void Save(BinaryWriter writer)
    {
        writer.Write((int)Type);
        writer.Write(InputSize);
        writer.Write(OutputSize);
        writer.Write(_hiddenSize);
        _wf.Save(writer); _wi.Save(writer); _wc.Save(writer); _wo.Save(writer);
        _uf.Save(writer); _ui.Save(writer); _uc.Save(writer); _uo.Save(writer);
        _outputWeights.Save(writer);
        foreach (var arr in new[] { _bf, _bi, _bc, _bo, _outputBias })
        {
            writer.Write(arr.Length);
            foreach (var v in arr) writer.Write(v);
        }
    }

    public override void Load(BinaryReader reader)
    {
        reader.ReadInt32();
        reader.ReadInt32();
        reader.ReadInt32();
        _ = reader.ReadInt32();
    }
}
