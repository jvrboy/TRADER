using System.Net.Http;

namespace Brain.Tools;

/// <summary>
/// Web search tool using DuckDuckGo Instant Answer API.
/// </summary>
public sealed class WebSearchTool : ITool
{
    private readonly HttpClient _httpClient;

    public WebSearchTool(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "BrainSystem/1.0");
    }

    public string Name => "WebSearch";
    public string Description => "Searches the web for information using DuckDuckGo Instant Answer API.";
    public string ParameterSchema => "{\"query\": \"string\"}";

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("query", out var queryObj) || queryObj is not string query)
            return ToolResult.Fail("Missing required parameter: query");

        var url = "https://api.duckduckgo.com/?q=" + Uri.EscapeDataString(query) + "&format=json&no_html=1&skip_disambig=1";

        try
        {
            var response = await _httpClient.GetStringAsync(url);
            var doc = System.Text.Json.JsonDocument.Parse(response);
            var sb = new System.Text.StringBuilder();

            if (doc.RootElement.TryGetProperty("AbstractText", out var abstractText) && !string.IsNullOrEmpty(abstractText.GetString()))
            {
                sb.AppendLine(abstractText.GetString());
            }
            if (doc.RootElement.TryGetProperty("AbstractURL", out var abstractUrl) && !string.IsNullOrEmpty(abstractUrl.GetString()))
            {
                sb.AppendLine("Source: " + abstractUrl.GetString());
            }

            if (doc.RootElement.TryGetProperty("RelatedTopics", out var topics))
            {
                foreach (var topic in topics.EnumerateArray().Take(5))
                {
                    if (topic.TryGetProperty("Text", out var text))
                        sb.AppendLine("- " + text.GetString());
                }
            }

            var result = sb.ToString();
            return string.IsNullOrEmpty(result)
                ? ToolResult.Ok("No results found for: " + query)
                : ToolResult.Ok(result);
        }
        catch (Exception ex)
        {
            return ToolResult.Fail("Search failed: " + ex.Message);
        }
    }
}
