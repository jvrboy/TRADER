using System;
using System.Runtime.CompilerServices;
using System.Numerics;

namespace BrainSystem.Core;

/// <summary>
/// A dense float tensor with SIMD-accelerated ops. Foundation for the whole brain.
/// </summary>
public sealed class Tensor
{
    public float[] Data;
    public int[] Shape;
    public int Length => Data.Length;

    public Tensor(params int[] shape)
    {
        Shape = shape;
        int n = 1;
        foreach (var s in shape) n *= s;
        Data = new float[n];
    }

    public Tensor(float[] data, params int[] shape)
    {
        Data = data;
        Shape = shape;
    }

    public float this[int i]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Data[i];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Data[i] = value;
    }

    public float this[int r, int c]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Data[r * Shape[1] + c];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Data[r * Shape[1] + c] = value;
    }

    public void Fill(float v) => Array.Fill(Data, v);

    public void RandomUniform(Random rng, float lo = -0.1f, float hi = 0.1f)
    {
        float range = hi - lo;
        for (int i = 0; i < Data.Length; i++)
            Data[i] = (float)rng.NextDouble() * range + lo;
    }

    public void RandomXavier(Random rng, int fanIn, int fanOut)
    {
        float bound = MathF.Sqrt(6f / (fanIn + fanOut));
        RandomUniform(rng, -bound, bound);
    }

    public Tensor Clone() => new Tensor((float[])Data.Clone(), (int[])Shape.Clone());

    // ---------- SIMD elementwise ops ----------
    public static void AddInPlace(float[] a, float[] b)
    {
        int i = 0;
        int simd = Vector<float>.Count;
        for (; i <= a.Length - simd; i += simd)
        {
            var va = new Vector<float>(a, i);
            var vb = new Vector<float>(b, i);
            (va + vb).CopyTo(a, i);
        }
        for (; i < a.Length; i++) a[i] += b[i];
    }

    public static void ScaleInPlace(float[] a, float s)
    {
        int i = 0;
        int simd = Vector<float>.Count;
        var vs = new Vector<float>(s);
        for (; i <= a.Length - simd; i += simd)
        {
            var va = new Vector<float>(a, i);
            (va * vs).CopyTo(a, i);
        }
        for (; i < a.Length; i++) a[i] *= s;
    }

    public static float Dot(float[] a, float[] b)
    {
        int i = 0;
        int simd = Vector<float>.Count;
        var acc = Vector<float>.Zero;
        for (; i <= a.Length - simd; i += simd)
        {
            var va = new Vector<float>(a, i);
            var vb = new Vector<float>(b, i);
            acc += va * vb;
        }
        float sum = 0;
        for (int k = 0; k < simd; k++) sum += acc[k];
        for (; i < a.Length; i++) sum += a[i] * b[i];
        return sum;
    }

    /// <summary>Matrix (rows x cols) * vector (cols) -> out (rows). SIMD row-major.</summary>
    public static void MatVec(float[] mat, float[] vec, float[] outv, int rows, int cols)
    {
        int simd = Vector<float>.Count;
        for (int r = 0; r < rows; r++)
        {
            int rowOff = r * cols;
            var acc = Vector<float>.Zero;
            int c = 0;
            for (; c <= cols - simd; c += simd)
            {
                var vm = new Vector<float>(mat, rowOff + c);
                var vv = new Vector<float>(vec, c);
                acc += vm * vv;
            }
            float sum = 0;
            for (int k = 0; k < simd; k++) sum += acc[k];
            for (; c < cols; c++) sum += mat[rowOff + c] * vec[c];
            outv[r] = sum;
        }
    }
}

public static class Activation
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ReLU(float x) => x > 0 ? x : 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float LeakyReLU(float x) => x > 0 ? x : 0.01f * x;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sigmoid(float x) => 1f / (1f + MathF.Exp(-x));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Tanh(float x) => MathF.Tanh(x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GELU(float x) => 0.5f * x * (1f + MathF.Tanh(0.79788456f * (x + 0.044715f * x * x * x)));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SiLU(float x) => x * Sigmoid(x);

    public static void Softmax(float[] v)
    {
        float max = float.NegativeInfinity;
        for (int i = 0; i < v.Length; i++) if (v[i] > max) max = v[i];
        float sum = 0;
        for (int i = 0; i < v.Length; i++) { v[i] = MathF.Exp(v[i] - max); sum += v[i]; }
        float inv = 1f / sum;
        for (int i = 0; i < v.Length; i++) v[i] *= inv;
    }

    public enum Kind { ReLU, LeakyReLU, Sigmoid, Tanh, GELU, SiLU, Linear }

    public static float Apply(Kind k, float x) => k switch
    {
        Kind.ReLU => ReLU(x),
        Kind.LeakyReLU => LeakyReLU(x),
        Kind.Sigmoid => Sigmoid(x),
        Kind.Tanh => Tanh(x),
        Kind.GELU => GELU(x),
        Kind.SiLU => SiLU(x),
        _ => x
    };

    public static float Derivative(Kind k, float y) => k switch
    {
        Kind.ReLU => y > 0 ? 1 : 0,
        Kind.LeakyReLU => y > 0 ? 1 : 0.01f,
        Kind.Sigmoid => y * (1 - y),
        Kind.Tanh => 1 - y * y,
        Kind.GELU => 1f,
        Kind.SiLU => 1f,
        _ => 1
    };
}
