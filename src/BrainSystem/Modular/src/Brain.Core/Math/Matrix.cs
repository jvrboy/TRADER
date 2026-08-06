using System.Numerics;
using System.Runtime.CompilerServices;

namespace Brain.Core.Math;

/// <summary>
/// High-performance matrix operations using SIMD (System.Numerics.Vector).
/// All operations work on float arrays with Span-based memory for cache efficiency.
/// </summary>
public sealed class Matrix
{
    public int Rows { get; }
    public int Cols { get; }
    public float[] Data { get; }

    public Matrix(int rows, int cols)
    {
        Rows = rows;
        Cols = cols;
        Data = new float[rows * cols];
    }

    public float this[int row, int col]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Data[row * Cols + col];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Data[row * Cols + col] = value;
    }

    /// <summary>
    /// Matrix-vector multiplication with SIMD acceleration.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Multiply(ReadOnlySpan<float> vector, Span<float> result)
    {
        var simdCount = Vector<float>.Count;
        for (int row = 0; row < Rows; row++)
        {
            var rowOffset = row * Cols;
            var sum = Vector<float>.Zero;
            int col;
            for (col = 0; col <= Cols - simdCount; col += simdCount)
            {
                var v1 = new Vector<float>(Data, rowOffset + col);
                var v2 = new Vector<float>(vector.Slice(col, simdCount));
                sum += v1 * v2;
            }
            var dot = Vector.Dot(sum, Vector<float>.One);
            for (; col < Cols; col++)
                dot += Data[rowOffset + col] * vector[col];
            result[row] = dot;
        }
    }

    /// <summary>
    /// Element-wise addition: result = a + b
    /// </summary>
    public static void Add(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result)
    {
        var simdCount = Vector<float>.Count;
        int i;
        for (i = 0; i <= a.Length - simdCount; i += simdCount)
        {
            var va = new Vector<float>(a.Slice(i, simdCount));
            var vb = new Vector<float>(b.Slice(i, simdCount));
            (va + vb).CopyTo(result.Slice(i, simdCount));
        }
        for (; i < a.Length; i++)
            result[i] = a[i] + b[i];
    }

    /// <summary>
    /// Fills the matrix with random values using Xavier/Glorot initialization.
    /// </summary>
    public void InitializeRandom(Random rng, float scale = 0.1f)
    {
        for (int i = 0; i < Data.Length; i++)
            Data[i] = (float)(rng.NextDouble() * 2 - 1) * scale;
    }

    /// <summary>
    /// Serializes the matrix to a binary stream for weight persistence.
    /// </summary>
    public void Save(BinaryWriter writer)
    {
        writer.Write(Rows);
        writer.Write(Cols);
        for (int i = 0; i < Data.Length; i++)
            writer.Write(Data[i]);
    }

    /// <summary>
    /// Deserializes the matrix from a binary stream.
    /// </summary>
    public static Matrix Load(BinaryReader reader)
    {
        var rows = reader.ReadInt32();
        var cols = reader.ReadInt32();
        var matrix = new Matrix(rows, cols);
        for (int i = 0; i < matrix.Data.Length; i++)
            matrix.Data[i] = reader.ReadSingle();
        return matrix;
    }
}
