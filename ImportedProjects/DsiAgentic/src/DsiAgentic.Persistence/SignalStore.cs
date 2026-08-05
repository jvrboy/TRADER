using System.Text.Json;
using DsiAgentic.Core;

namespace DsiAgentic.Persistence;

public sealed class SignalStore
{
    private readonly string _openPath;
    private readonly string _historyPath;
    private readonly JsonSerializerOptions _opts = new() { WriteIndented = true };

    public SignalStore(string dataDir)
    {
        Directory.CreateDirectory(dataDir);
        _openPath = Path.Combine(dataDir, "open_signals.json");
        _historyPath = Path.Combine(dataDir, "signals.jsonl");
    }

    public List<Signal> LoadOpen()
    {
        if (!File.Exists(_openPath)) return new();
        try { return JsonSerializer.Deserialize<List<Signal>>(File.ReadAllText(_openPath)) ?? new(); }
        catch { return new(); }
    }

    public void SaveOpen(List<Signal> sigs) => File.WriteAllText(_openPath, JsonSerializer.Serialize(sigs, _opts));

    public void AppendHistory(Signal s) => File.AppendAllText(_historyPath, JsonSerializer.Serialize(s) + "\n");

    public IEnumerable<Signal> ReadHistory(int lastN = 50)
    {
        if (!File.Exists(_historyPath)) yield break;
        var lines = File.ReadAllLines(_historyPath);
        int start = Math.Max(0, lines.Length - lastN);
        for (int i = start; i < lines.Length; i++)
        {
            Signal? s = null;
            try { s = JsonSerializer.Deserialize<Signal>(lines[i]); } catch { }
            if (s != null) yield return s;
        }
    }
}
