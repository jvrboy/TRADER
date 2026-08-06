using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BrainSystem.LLM;

/// <summary>
/// Native C# GGUF v3 file parser (llama.cpp format). Reads header, KV metadata, tensor index.
/// Tensor data is memory-mapped on demand.
/// </summary>
public enum GgufValueType : uint
{
    Uint8 = 0, Int8 = 1, Uint16 = 2, Int16 = 3, Uint32 = 4, Int32 = 5,
    Float32 = 6, Bool = 7, String = 8, Array = 9, Uint64 = 10, Int64 = 11, Float64 = 12
}

public enum GgmlType : uint
{
    F32 = 0, F16 = 1, Q4_0 = 2, Q4_1 = 3, Q5_0 = 6, Q5_1 = 7, Q8_0 = 8, Q8_1 = 9,
    Q2_K = 10, Q3_K = 11, Q4_K = 12, Q5_K = 13, Q6_K = 14, Q8_K = 15,
    IQ2_XXS = 16, IQ2_XS = 17, IQ3_XXS = 18, IQ1_S = 19, IQ4_NL = 20, IQ3_S = 21, IQ2_S = 22, IQ4_XS = 23,
    BF16 = 30, I8 = 24, I16 = 25, I32 = 26, I64 = 27, F64 = 28
}

public class GgufTensorInfo
{
    public string Name = "";
    public ulong[] Shape = Array.Empty<ulong>();
    public GgmlType Type;
    public ulong Offset;
    public ulong ByteSize;
}

public class GgufFile : IDisposable
{
    public string Path { get; }
    public uint Version { get; private set; }
    public ulong TensorCount { get; private set; }
    public ulong MetaKvCount { get; private set; }
    public Dictionary<string, object> Metadata { get; } = new();
    public List<GgufTensorInfo> Tensors { get; } = new();
    public ulong DataStart { get; private set; }
    public ulong Alignment { get; private set; } = 32;

    private readonly FileStream _fs;
    private readonly BinaryReader _br;

    public GgufFile(string path)
    {
        Path = path;
        _fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        _br = new BinaryReader(_fs);
        Parse();
    }

    void Parse()
    {
        var magic = _br.ReadUInt32();
        if (magic != 0x46554747u) throw new InvalidDataException($"Not GGUF (magic=0x{magic:X8})");
        Version = _br.ReadUInt32();
        if (Version < 2 || Version > 3) throw new InvalidDataException($"Unsupported GGUF version {Version}");
        TensorCount = _br.ReadUInt64();
        MetaKvCount = _br.ReadUInt64();

        for (ulong i = 0; i < MetaKvCount; i++)
        {
            var key = ReadString();
            var vt = (GgufValueType)_br.ReadUInt32();
            var val = ReadValue(vt);
            Metadata[key] = val;
        }
        if (Metadata.TryGetValue("general.alignment", out var a) && a is uint au) Alignment = au;

        for (ulong i = 0; i < TensorCount; i++)
        {
            var t = new GgufTensorInfo();
            t.Name = ReadString();
            uint nDims = _br.ReadUInt32();
            t.Shape = new ulong[nDims];
            for (uint d = 0; d < nDims; d++) t.Shape[d] = _br.ReadUInt64();
            t.Type = (GgmlType)_br.ReadUInt32();
            t.Offset = _br.ReadUInt64();
            Tensors.Add(t);
        }

        // Align data start
        ulong pos = (ulong)_fs.Position;
        ulong pad = (Alignment - (pos % Alignment)) % Alignment;
        DataStart = pos + pad;

        // Compute per-tensor byte sizes for reporting
        foreach (var t in Tensors) t.ByteSize = ComputeByteSize(t);
    }

    static ulong ComputeByteSize(GgufTensorInfo t)
    {
        ulong n = 1; foreach (var s in t.Shape) n *= s;
        return t.Type switch
        {
            GgmlType.F32 => n * 4,
            GgmlType.F16 or GgmlType.BF16 => n * 2,
            GgmlType.F64 => n * 8,
            GgmlType.I8 => n,
            GgmlType.I16 => n * 2,
            GgmlType.I32 => n * 4,
            GgmlType.I64 => n * 8,
            GgmlType.Q8_0 => (n / 32) * 34,
            GgmlType.Q4_0 => (n / 32) * 18,
            GgmlType.Q4_1 => (n / 32) * 20,
            GgmlType.Q5_0 => (n / 32) * 22,
            GgmlType.Q5_1 => (n / 32) * 24,
            GgmlType.Q4_K => (n / 256) * 144,
            GgmlType.Q5_K => (n / 256) * 176,
            GgmlType.Q6_K => (n / 256) * 210,
            GgmlType.Q2_K => (n / 256) * 84,
            GgmlType.Q3_K => (n / 256) * 110,
            GgmlType.Q8_K => (n / 256) * 292,
            _ => n * 2, // conservative
        };
    }

    string ReadString()
    {
        ulong len = _br.ReadUInt64();
        var bytes = _br.ReadBytes((int)len);
        return Encoding.UTF8.GetString(bytes);
    }

    object ReadValue(GgufValueType vt)
    {
        return vt switch
        {
            GgufValueType.Uint8 => (object)_br.ReadByte(),
            GgufValueType.Int8 => _br.ReadSByte(),
            GgufValueType.Uint16 => _br.ReadUInt16(),
            GgufValueType.Int16 => _br.ReadInt16(),
            GgufValueType.Uint32 => _br.ReadUInt32(),
            GgufValueType.Int32 => _br.ReadInt32(),
            GgufValueType.Float32 => _br.ReadSingle(),
            GgufValueType.Bool => _br.ReadByte() != 0,
            GgufValueType.String => ReadString(),
            GgufValueType.Uint64 => _br.ReadUInt64(),
            GgufValueType.Int64 => _br.ReadInt64(),
            GgufValueType.Float64 => _br.ReadDouble(),
            GgufValueType.Array => ReadArray(),
            _ => throw new InvalidDataException($"Unknown vt {vt}")
        };
    }

    object ReadArray()
    {
        var innerType = (GgufValueType)_br.ReadUInt32();
        ulong len = _br.ReadUInt64();
        var arr = new object[len];
        for (ulong i = 0; i < len; i++) arr[i] = ReadValue(innerType);
        return arr;
    }

    /// <summary>Read raw tensor bytes on demand.</summary>
    public byte[] ReadTensorBytes(GgufTensorInfo t)
    {
        _fs.Position = (long)(DataStart + t.Offset);
        var buf = new byte[t.ByteSize];
        int read = 0;
        while (read < buf.Length)
        {
            int n = _fs.Read(buf, read, buf.Length - read);
            if (n <= 0) break; read += n;
        }
        return buf;
    }

    public string ArchName => Metadata.TryGetValue("general.architecture", out var v) ? v.ToString() ?? "unknown" : "unknown";
    public string ModelName => Metadata.TryGetValue("general.name", out var v) ? v.ToString() ?? "unnamed" : "unnamed";

    public void Dispose() { _br.Dispose(); _fs.Dispose(); }
}
