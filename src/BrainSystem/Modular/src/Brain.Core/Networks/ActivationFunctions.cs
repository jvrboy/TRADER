using System.Runtime.CompilerServices;

namespace Brain.Core.Networks;

/// <summary>
/// Vectorized activation functions for neural networks.
/// </summary>
public static class ActivationFunctions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ReLU(float x) => Math.Max(0, x);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ReLUDerivative(float x) => x > 0 ? 1f : 0f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Tanh(float x) => MathF.Tanh(x);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float TanhDerivative(float x)
    {
        var t = MathF.Tanh(x);
        return 1f - t * t;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sigmoid(float x) => 1f / (1f + MathF.Exp(-x));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SigmoidDerivative(float x)
    {
        var s = Sigmoid(x);
        return s * (1f - s);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float LeakyReLU(float x, float alpha = 0.01f) => x > 0 ? x : alpha * x;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float LeakyReLUDerivative(float x, float alpha = 0.01f) => x > 0 ? 1f : alpha;

    /// <summary>
    /// Applies an activation function to an entire span in-place.
    /// </summary>
    public static void Apply(Span<float> values, ActivationType type)
    {
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = type switch
            {
                ActivationType.ReLU => ReLU(values[i]),
                ActivationType.Tanh => Tanh(values[i]),
                ActivationType.Sigmoid => Sigmoid(values[i]),
                ActivationType.LeakyReLU => LeakyReLU(values[i]),
                _ => values[i]
            };
        }
    }
}

public enum ActivationType
{
    ReLU,
    Tanh,
    Sigmoid,
    LeakyReLU,
    Linear
}
