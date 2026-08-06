using System.Text.Json;

namespace Brain.LLM;

/// <summary>
/// Parses JSON-formatted tool calls from LLM output.
/// Expected format: {"tool": "WebSearch", "params": {"query": "..."}}
/// </summary>
public static class ToolCallParser
{
    /// <summary>
    /// Extracts tool calls from LLM text output.
    /// Looks for JSON blocks matching the tool call format.
    /// </summary>
    public static List<ToolCall> Parse(string llmOutput)
    {
        var calls = new List<ToolCall>();
        var jsonBlocks = ExtractJsonBlocks(llmOutput);

        foreach (var block in jsonBlocks)
        {
            try
            {
                var doc = JsonDocument.Parse(block);
                if (doc.RootElement.TryGetProperty("tool", out var toolProp) &&
                    doc.RootElement.TryGetProperty("params", out var paramsProp))
                {
                    var toolName = toolProp.GetString() ?? string.Empty;
                    var parameters = new Dictionary<string, object>();
                    foreach (var prop in paramsProp.EnumerateObject())
                    {
                        parameters[prop.Name] = prop.Value.ValueKind switch
                        {
                            JsonValueKind.String => prop.Value.GetString() ?? "",
                            JsonValueKind.Number => prop.Value.GetDouble(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            _ => prop.Value.GetRawText()
                        };
                    }
                    calls.Add(new ToolCall(toolName, parameters));
                }
            }
            catch
            {
            }
        }

        return calls;
    }

    private static List<string> ExtractJsonBlocks(string text)
    {
        var blocks = new List<string>();
        var start = -1;
        var depth = 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                if (depth == 0) start = i;
                depth++;
            }
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0 && start >= 0)
                {
                    blocks.Add(text.Substring(start, i - start + 1));
                    start = -1;
                }
            }
        }

        return blocks;
    }
}

public sealed record ToolCall(string Tool, Dictionary<string, object> Parameters);
