namespace NexusBrain.Core;

/// <summary>A trading signal produced by an agent, with confidence and metadata.</summary>
public sealed class Signal
{
    public required string Symbol { get; init; }
    public required Bias Bias { get; init; }
    public double Confidence { get; set; }          // 0..1
    public double Strength { get; set; }            // magnitude, e.g. z-score
    public required string Agent { get; init; }     // which agent produced it
    public required string Reason { get; init; }    // human-readable explanation
    public string? Entry { get; init; }
    public string? Target { get; init; }
    public string? Stop { get; init; }
    public long Epoch { get; init; }
    public Dictionary<string, double> Metrics { get; init; } = new();

    public override string ToString()
        => $"[{Agent}] {Symbol} {(Bias == Bias.Bullish ? "LONG" : Bias == Bias.Bearish ? "SHORT" : "FLAT")} conf={Confidence:P0} :: {Reason}";
}

/// <summary>An analysis result bundle from the whole brain (all agents).</summary>
public sealed class BrainAnalysis
{
    public long Epoch { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    public required string Symbol { get; init; }
    public required InstrumentKind Kind { get; init; }
    public Bias AggregateBias { get; set; }
    public double AggregateConfidence { get; set; }
    public List<Signal> Signals { get; init; } = new();
    public Dictionary<string, double> Regime { get; init; } = new();
    public Dictionary<string, string> Notes { get; init; } = new();
}

/// <summary>Configuration for a brain instance (paths, tuning knobs).</summary>
public sealed class BrainConfig
{
    public string DataRoot { get; set; } = "data";
    public string BrainDir => Path.Combine(DataRoot, "brains");
    public string KnowledgeDir => Path.Combine(DataRoot, "knowledge");
    public string SignalsDir => Path.Combine(DataRoot, "signals");
    public string ModelsDir => Path.Combine(DataRoot, "models");

    public string DerivWsUrl { get; set; } = "wss://ws.derivws.com/websockets/v3?app_id=1089";
    public int AppId { get; set; } = 1089;

    public double LearningRate { get; set; } = 0.02;
    public double Momentum { get; set; } = 0.9;
    public int HiddenUnits { get; set; } = 64;
    public double ExplorationEpsilon { get; set; } = 0.1;
    public int WorkingMemorySlots { get; set; } = 128;
    public int MaxEpisodicMemories { get; set; } = 10000;

    public void EnsureDirectories()
    {
        foreach (var d in new[] { BrainDir, KnowledgeDir, SignalsDir, ModelsDir })
            Directory.CreateDirectory(d);
    }
}
