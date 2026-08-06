namespace Brain.Memory;

/// <summary>
/// Consolidation service: periodically moves high-relevance STM entries to LTM.
/// Runs on a background timer every 10 minutes by default.
/// </summary>
public sealed class MemoryConsolidator : IDisposable
{
    private readonly ShortTermMemory _stm;
    private readonly LongTermMemory _ltm;
    private readonly Timer _timer;
    private readonly Func<string, float[]>? _embeddingFunc;

    public MemoryConsolidator(ShortTermMemory stm, LongTermMemory ltm,
        Func<string, float[]>? embeddingFunc = null, TimeSpan? interval = null)
    {
        _stm = stm;
        _ltm = ltm;
        _embeddingFunc = embeddingFunc;
        var period = interval ?? TimeSpan.FromMinutes(10);
        _timer = new Timer(_ => Consolidate(), null, period, period);
    }

    /// <summary>
    /// Consolidates high-relevance STM entries into LTM.
    /// </summary>
    public void Consolidate()
    {
        var highRelevance = _stm.GetHighRelevance(0.7f);
        foreach (var entry in highRelevance)
        {
            if (entry.Embedding == null && _embeddingFunc != null)
            {
                entry.Embedding = _embeddingFunc(entry.Content);
            }
            if (entry.Embedding != null)
            {
                _ltm.Store(entry);
            }
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
