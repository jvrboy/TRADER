using Brain.Core.Math;
using Xunit;

namespace Brain.Tests;

public class MatrixTests
{
    [Fact]
    public void Matrix_Create_HasCorrectDimensions()
    {
        var matrix = new Matrix(3, 4);
        Assert.Equal(3, matrix.Rows);
        Assert.Equal(4, matrix.Cols);
        Assert.Equal(12, matrix.Data.Length);
    }

    [Fact]
    public void Matrix_Multiply_ProducesCorrectResult()
    {
        var matrix = new Matrix(2, 3);
        matrix[0, 0] = 1; matrix[0, 1] = 2; matrix[0, 2] = 3;
        matrix[1, 0] = 4; matrix[1, 1] = 5; matrix[1, 2] = 6;

        var vector = new float[] { 1, 2, 3 };
        var result = new float[2];
        matrix.Multiply(vector, result);

        Assert.Equal(14, result[0], precision: 2);  // 1*1 + 2*2 + 3*3
        Assert.Equal(32, result[1], precision: 2);  // 4*1 + 5*2 + 6*3
    }

    [Fact]
    public void Matrix_Add_ProducesCorrectResult()
    {
        var a = new float[] { 1, 2, 3, 4 };
        var b = new float[] { 5, 6, 7, 8 };
        var result = new float[4];
        Matrix.Add(a, b, result);

        Assert.Equal(new float[] { 6, 8, 10, 12 }, result);
    }

    [Fact]
    public void Matrix_InitializeRandom_ProducesValuesInRange()
    {
        var matrix = new Matrix(10, 10);
        matrix.InitializeRandom(new Random(42), 0.5f);

        foreach (var v in matrix.Data)
        {
            Assert.True(v >= -0.5f && v <= 0.5f);
        }
    }

    [Fact]
    public void Matrix_SaveLoad_RoundTripsCorrectly()
    {
        var matrix = new Matrix(3, 3);
        matrix[0, 0] = 1.5f; matrix[1, 1] = 2.5f; matrix[2, 2] = 3.5f;

        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms))
        {
            matrix.Save(writer);
        }
        ms.Position = 0;
        using var reader = new BinaryReader(ms);
        var loaded = Matrix.Load(reader);

        Assert.Equal(matrix.Rows, loaded.Rows);
        Assert.Equal(matrix.Cols, loaded.Cols);
        Assert.Equal(1.5f, loaded[0, 0]);
        Assert.Equal(2.5f, loaded[1, 1]);
        Assert.Equal(3.5f, loaded[2, 2]);
    }
}
