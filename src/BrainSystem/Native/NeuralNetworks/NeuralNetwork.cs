using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BrainSystem.Core;

namespace BrainSystem.NeuralNetworks;

/// <summary>
/// A general-purpose dense MLP with backprop. The "atom" of the 1000+ brain network.
/// </summary>
public class NeuralNetwork
{
    public string Id;
    public string Name;
    public string Purpose;
    public int[] Layers;                    // e.g. [16, 32, 8]
    public Activation.Kind[] Activations;
    public float[][] Weights;                // Weights[l] = row-major [outSize * inSize]
    public float[][] Biases;
    public float[][] LastActivations;        // for backprop
    public float LearningRate = 0.01f;
    public long TrainingSteps;
    public double TotalLoss;

    public NeuralNetwork(string name, string purpose, int[] layers, Activation.Kind[]? activations = null, int? seed = null)
    {
        Id = Guid.NewGuid().ToString("N")[..8];
        Name = name;
        Purpose = purpose;
        Layers = layers;
        Activations = activations ?? DefaultActivations(layers.Length - 1);
        Weights = new float[layers.Length - 1][];
        Biases = new float[layers.Length - 1][];
        LastActivations = new float[layers.Length][];
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        for (int i = 0; i < layers.Length - 1; i++)
        {
            int fi = layers[i], fo = layers[i + 1];
            var w = new Tensor(fo, fi);
            w.RandomXavier(rng, fi, fo);
            Weights[i] = w.Data;
            Biases[i] = new float[fo];
        }
        for (int i = 0; i < layers.Length; i++)
            LastActivations[i] = new float[layers[i]];
    }

    static Activation.Kind[] DefaultActivations(int n)
    {
        var a = new Activation.Kind[n];
        for (int i = 0; i < n - 1; i++) a[i] = Activation.Kind.GELU;
        a[n - 1] = Activation.Kind.Linear;
        return a;
    }

    public float[] Forward(float[] input)
    {
        Array.Copy(input, LastActivations[0], input.Length);
        for (int l = 0; l < Weights.Length; l++)
        {
            int inSize = Layers[l];
            int outSize = Layers[l + 1];
            var outv = LastActivations[l + 1];
            Tensor.MatVec(Weights[l], LastActivations[l], outv, outSize, inSize);
            var bias = Biases[l];
            for (int i = 0; i < outSize; i++)
                outv[i] = Activation.Apply(Activations[l], outv[i] + bias[i]);
        }
        return LastActivations[^1];
    }

    /// <summary>Simple MSE backprop step. Returns loss.</summary>
    public float TrainStep(float[] input, float[] target)
    {
        var pred = Forward(input);
        int L = Weights.Length;
        var grads = new float[L + 1][];
        for (int i = 0; i <= L; i++) grads[i] = new float[Layers[i]];

        // Output gradient (MSE)
        float loss = 0;
        var gOut = grads[L];
        for (int i = 0; i < pred.Length; i++)
        {
            float diff = pred[i] - target[i];
            loss += diff * diff;
            gOut[i] = 2f * diff * Activation.Derivative(Activations[L - 1], pred[i]);
        }

        // Backprop
        for (int l = L - 1; l >= 0; l--)
        {
            int inSize = Layers[l];
            int outSize = Layers[l + 1];
            var w = Weights[l];
            var b = Biases[l];
            var actIn = LastActivations[l];
            var gO = grads[l + 1];
            var gI = grads[l];

            for (int j = 0; j < inSize; j++)
            {
                float s = 0;
                for (int i = 0; i < outSize; i++) s += w[i * inSize + j] * gO[i];
                gI[j] = l > 0 ? s * Activation.Derivative(Activations[l - 1], actIn[j]) : s;
            }

            for (int i = 0; i < outSize; i++)
            {
                float go = gO[i];
                for (int j = 0; j < inSize; j++)
                    w[i * inSize + j] -= LearningRate * go * actIn[j];
                b[i] -= LearningRate * go;
            }
        }

        TrainingSteps++;
        TotalLoss += loss;
        return loss;
    }

    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write("BNN1");
        bw.Write(Id); bw.Write(Name); bw.Write(Purpose);
        bw.Write(Layers.Length);
        foreach (var l in Layers) bw.Write(l);
        foreach (var a in Activations) bw.Write((int)a);
        for (int i = 0; i < Weights.Length; i++)
        {
            bw.Write(Weights[i].Length);
            foreach (var v in Weights[i]) bw.Write(v);
            bw.Write(Biases[i].Length);
            foreach (var v in Biases[i]) bw.Write(v);
        }
        bw.Write(TrainingSteps);
        return ms.ToArray();
    }

    public static NeuralNetwork Deserialize(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var br = new BinaryReader(ms);
        if (br.ReadString() != "BNN1") throw new InvalidDataException("Bad magic");
        var id = br.ReadString(); var name = br.ReadString(); var purpose = br.ReadString();
        int nL = br.ReadInt32();
        var layers = new int[nL];
        for (int i = 0; i < nL; i++) layers[i] = br.ReadInt32();
        var acts = new Activation.Kind[nL - 1];
        for (int i = 0; i < nL - 1; i++) acts[i] = (Activation.Kind)br.ReadInt32();
        var nn = new NeuralNetwork(name, purpose, layers, acts);
        nn.Id = id;
        for (int i = 0; i < nn.Weights.Length; i++)
        {
            int wl = br.ReadInt32();
            for (int k = 0; k < wl; k++) nn.Weights[i][k] = br.ReadSingle();
            int bl = br.ReadInt32();
            for (int k = 0; k < bl; k++) nn.Biases[i][k] = br.ReadSingle();
        }
        nn.TrainingSteps = br.ReadInt64();
        return nn;
    }
}
