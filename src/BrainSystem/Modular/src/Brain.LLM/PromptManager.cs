namespace Brain.LLM;

/// <summary>
/// Manages prompt templates and context window for LLM conversations.
/// Implements sliding window with summarization for long conversations.
/// </summary>
public sealed class PromptManager
{
    private readonly int _maxContextTokens;
    private readonly List<ChatMessage> _history = new();
    private string _systemPrompt = "You are a helpful AI assistant with access to tools.";

    public PromptManager(int maxContextTokens = 4096)
    {
        _maxContextTokens = maxContextTokens;
    }

    public void SetSystemPrompt(string prompt) => _systemPrompt = prompt;

    public void AddMessage(string role, string content)
    {
        _history.Add(new ChatMessage(role, content, DateTime.UtcNow));
        PruneContext();
    }

    /// <summary>
    /// Builds the full prompt from system prompt and conversation history.
    /// </summary>
    public string BuildPrompt()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("System: " + _systemPrompt);

        foreach (var msg in _history)
        {
            sb.AppendLine(msg.Role + ": " + msg.Content);
        }

        sb.Append("Assistant: ");
        return sb.ToString();
    }

    /// <summary>
    /// Prunes the context window by summarizing older messages.
    /// </summary>
    private void PruneContext()
    {
        var estimatedTokens = _history.Sum(m => m.Content.Length / 4);
        while (estimatedTokens > _maxContextTokens && _history.Count > 2)
        {
            var oldest = _history[0];
            _history.RemoveAt(0);
            var summary = "[Summary: " + oldest.Content.Substring(0, Math.Min(50, oldest.Content.Length)) + "...]";
            _history.Insert(0, new ChatMessage("system", summary, DateTime.UtcNow));
            estimatedTokens = _history.Sum(m => m.Content.Length / 4);
        }
    }

    public void ClearHistory() => _history.Clear();

    public IReadOnlyList<ChatMessage> GetHistory() => _history.AsReadOnly();
}

public sealed record ChatMessage(string Role, string Content, DateTime Timestamp);
