using System.Text.Json;
using DsiAgentic.Core;

namespace DsiAgentic.Persistence;

public sealed class PerformanceStore
{
    private readonly string _path;
    public Dictionary<string, PerformanceRecord> Data { get; private set; } = new();

    public PerformanceStore(string dataDir)
    {
        Directory.CreateDirectory(dataDir);
        _path = Path.Combine(dataDir, "performance.json");
        Load();
    }

    public void Load()
    {
        if (!File.Exists(_path)) return;
        try { Data = JsonSerializer.Deserialize<Dictionary<string, PerformanceRecord>>(File.ReadAllText(_path)) ?? new(); }
        catch { Data = new(); }
    }

    public void Record(Signal sig)
    {
        if (sig.Status != SignalStatus.TpHit && sig.Status != SignalStatus.SlHit) return;
        if (!Data.TryGetValue(sig.Instrument, out var rec)) rec = new PerformanceRecord { Instrument = sig.Instrument };
        rec.Total++;
        if (sig.Status == SignalStatus.TpHit) rec.TpCount++; else rec.SlCount++;
        rec.NetPips += sig.PnlPips ?? 0;
        rec.LastUpdatedEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Data[sig.Instrument] = rec;
        Save();
    }

    public void Save() => File.WriteAllText(_path, JsonSerializer.Serialize(Data, new JsonSerializerOptions { WriteIndented = true }));
}
