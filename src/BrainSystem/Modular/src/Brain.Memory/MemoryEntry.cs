using System.Text.Json;

namespace Brain.Memory;

/// <summary>
/// A single memory entry stored in short-term or long-term memory.
/// </summary>
public sealed class MemoryEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string Content { get; init; } = string.Empty;
    public float[]? Embedding { get; set; }
    public float RelevanceScore { get; set; }
    public string Source { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = new();

    public string ToJson() => JsonSerializer.Serialize(this);
    public static MemoryEntry? FromJson(string json) => JsonSerializer.Deserialize<MemoryEntry>(json);
}
