using System;

namespace BrainSystem.LLM;

/// <summary>Dequantisation kernels for common GGUF tensor types → float[].</summary>
public static class GgufDequant
{
    public static float[] Dequantize(byte[] data, GgmlType type, long numel)
    {
        return type switch
        {
            GgmlType.F32 => AsF32(data, numel),
            GgmlType.F16 => AsF16(data, numel),
            GgmlType.BF16 => AsBF16(data, numel),
            GgmlType.Q8_0 => DequantQ8_0(data, numel),
            GgmlType.Q4_0 => DequantQ4_0(data, numel),
            GgmlType.Q4_1 => DequantQ4_1(data, numel),
            _ => throw new NotSupportedException($"Dequant not implemented for {type} (yet)")
        };
    }

    static float[] AsF32(byte[] data, long n)
    {
        var o = new float[n];
        Buffer.BlockCopy(data, 0, o, 0, (int)(n * 4));
        return o;
    }

    static float HalfToFloat(ushort h)
    {
        uint sign = (uint)((h >> 15) & 1);
        uint exp = (uint)((h >> 10) & 0x1F);
        uint mant = (uint)(h & 0x3FF);
        uint f;
        if (exp == 0)
        {
            if (mant == 0) f = sign << 31;
            else
            {
                exp = 1;
                while ((mant & 0x400) == 0) { mant <<= 1; exp--; }
                mant &= 0x3FF;
                f = (sign << 31) | ((exp + 112) << 23) | (mant << 13);
            }
        }
        else if (exp == 31)
            f = (sign << 31) | (0xFF << 23) | (mant << 13);
        else
            f = (sign << 31) | ((exp + 112) << 23) | (mant << 13);
        return BitConverter.Int32BitsToSingle((int)f);
    }

    static float[] AsF16(byte[] data, long n)
    {
        var o = new float[n];
        for (long i = 0; i < n; i++)
        {
            ushort h = (ushort)(data[i * 2] | (data[i * 2 + 1] << 8));
            o[i] = HalfToFloat(h);
        }
        return o;
    }

    static float[] AsBF16(byte[] data, long n)
    {
        var o = new float[n];
        for (long i = 0; i < n; i++)
        {
            uint u = (uint)(data[i * 2] | (data[i * 2 + 1] << 8));
            o[i] = BitConverter.Int32BitsToSingle((int)(u << 16));
        }
        return o;
    }

    // Q8_0: block of 32 elements, layout: fp16 scale + 32 int8 quants
    static float[] DequantQ8_0(byte[] data, long n)
    {
        var o = new float[n];
        int nb = (int)(n / 32);
        for (int b = 0; b < nb; b++)
        {
            int off = b * 34;
            ushort sh = (ushort)(data[off] | (data[off + 1] << 8));
            float d = HalfToFloat(sh);
            for (int j = 0; j < 32; j++)
                o[b * 32 + j] = ((sbyte)data[off + 2 + j]) * d;
        }
        return o;
    }

    // Q4_0: block of 32, fp16 scale + 16 packed nibbles (values in -8..7)
    static float[] DequantQ4_0(byte[] data, long n)
    {
        var o = new float[n];
        int nb = (int)(n / 32);
        for (int b = 0; b < nb; b++)
        {
            int off = b * 18;
            ushort sh = (ushort)(data[off] | (data[off + 1] << 8));
            float d = HalfToFloat(sh);
            for (int j = 0; j < 16; j++)
            {
                byte q = data[off + 2 + j];
                int q0 = (q & 0x0F) - 8;
                int q1 = (q >> 4) - 8;
                o[b * 32 + j] = q0 * d;
                o[b * 32 + j + 16] = q1 * d;
            }
        }
        return o;
    }

    // Q4_1: block of 32, fp16 scale, fp16 min, 16 packed nibbles
    static float[] DequantQ4_1(byte[] data, long n)
    {
        var o = new float[n];
        int nb = (int)(n / 32);
        for (int b = 0; b < nb; b++)
        {
            int off = b * 20;
            ushort sd = (ushort)(data[off] | (data[off + 1] << 8));
            ushort sm = (ushort)(data[off + 2] | (data[off + 3] << 8));
            float d = HalfToFloat(sd);
            float m = HalfToFloat(sm);
            for (int j = 0; j < 16; j++)
            {
                byte q = data[off + 4 + j];
                o[b * 32 + j] = (q & 0x0F) * d + m;
                o[b * 32 + j + 16] = (q >> 4) * d + m;
            }
        }
        return o;
    }
}
