using System.Text.Json;
using DsiAgentic.Core;

namespace DsiAgentic.Learning;

/// <summary>
/// Append-only knowledge store: every closed signal is written to
/// data/knowledge/{instrument}.jsonl with features, outcome, and vote breakdown
/// so brains can be retrained from the raw history at any time.
/// </summary>
public sealed class KnowledgeStore
{
    private readonly string _dir;
    public KnowledgeStore(string dir) { _dir = dir; Directory.CreateDirectory(_dir); }

    public void Append(Signal closed)
    {
        var path = Path.Combine(_dir, $"{closed.Instrument}.jsonl");
        var entry = new
        {
            id = closed.Id,
            ts = closed.CreatedEpoch,
            closed_ts = closed.ClosedEpoch,
            status = closed.Status.ToString(),
            direction = closed.Direction.ToString(),
            entry = closed.Entry, tp = closed.TakeProfit, sl = closed.StopLoss,
            confluence = closed.Confluence,
            bull = closed.BullScore, bear = closed.BearScore,
            regime = closed.Regime.ToString(),
            features = closed.Features,
            votes = closed.Votes.Select(v => new { v.Agent, v.Family, v.Direction, v.Weight, v.Confidence, v.Reason })
        };
        var line = JsonSerializer.Serialize(entry);
        File.AppendAllText(path, line + "\n");
    }

    public IEnumerable<string> Read(string instrument, int lastN = 100)
    {
        var path = Path.Combine(_dir, $"{instrument}.jsonl");
        if (!File.Exists(path)) yield break;
        var lines = File.ReadAllLines(path);
        int start = Math.Max(0, lines.Length - lastN);
        for (int i = start; i < lines.Length; i++) yield return lines[i];
    }
}
