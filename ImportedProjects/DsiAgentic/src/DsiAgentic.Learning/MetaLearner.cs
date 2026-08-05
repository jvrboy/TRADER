using System.Text.Json;
using DsiAgentic.Core;

namespace DsiAgentic.Learning;

/// <summary>
/// Tracks per-agent and per-strategy win-rates and returns a multiplier used
/// to weight votes at signal-time. Persisted as JSON.
/// </summary>
public sealed class MetaLearner
{
    public sealed class Stat
    {
        public int Total { get; set; }
        public int Wins { get; set; }
        public double Weight { get; set; } = 1.0;
        public double WinRate => Total == 0 ? 0.5 : (double)Wins / Total;
    }

    public Dictionary<string, Stat> AgentStats { get; set; } = new();
    public Dictionary<string, Stat> FamilyStats { get; set; } = new();

    public double AgentMultiplier(string agent) =>
        AgentStats.TryGetValue(agent, out var s) ? 0.5 + s.WinRate : 1.0;

    public double FamilyMultiplier(string family) =>
        FamilyStats.TryGetValue(family, out var s) ? 0.5 + s.WinRate : 1.0;

    public void Record(Signal s, bool won)
    {
        foreach (var v in s.Votes)
        {
            var a = AgentStats.GetValueOrDefault(v.Agent, new Stat());
            a.Total++; if (won) a.Wins++;
            a.Weight = 0.5 + a.WinRate;
            AgentStats[v.Agent] = a;

            var f = FamilyStats.GetValueOrDefault(v.Family, new Stat());
            f.Total++; if (won) f.Wins++;
            f.Weight = 0.5 + f.WinRate;
            FamilyStats[v.Family] = f;
        }
    }

    public static MetaLearner LoadOrNew(string path)
    {
        if (!File.Exists(path)) return new MetaLearner();
        try { return JsonSerializer.Deserialize<MetaLearner>(File.ReadAllText(path)) ?? new MetaLearner(); }
        catch { return new MetaLearner(); }
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
