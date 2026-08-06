using System.Net.Http;
using System.Xml.Linq;

namespace Brain.Tools;

/// <summary>
/// News summary tool: fetches RSS feeds and summarizes headlines.
/// </summary>
public sealed class NewsSummaryTool : ITool
{
    private readonly HttpClient _httpClient;

    public NewsSummaryTool(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "BrainSystem/1.0");
    }

    public string Name => "NewsSummary";
    public string Description => "Fetches recent news headlines from RSS feeds and returns a summary.";
    public string ParameterSchema => "{\"topic\": \"string?\"}";

    private static readonly string[] DefaultFeeds =
    {
        "https://feeds.reuters.com/reuters/businessNews",
        "https://feeds.bbci.co.uk/news/business/rss.xml"
    };

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var topic = parameters.TryGetValue("topic", out var t) ? t.ToString() : "general";

        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("News summary for: " + topic);

            foreach (var feedUrl in DefaultFeeds)
            {
                try
                {
                    var content = await _httpClient.GetStringAsync(feedUrl);
                    var doc = XDocument.Parse(content);
                    var items = doc.Descendants("item").Take(5);

                    foreach (var item in items)
                    {
                        var title = item.Element("title")?.Value ?? "";
                        var pubDate = item.Element("pubDate")?.Value ?? "";
                        sb.AppendLine("- [" + pubDate + "] " + title);
                    }
                }
                catch
                {
                }
            }

            return ToolResult.Ok(sb.ToString());
        }
        catch (Exception ex)
        {
            return ToolResult.Fail("News fetch failed: " + ex.Message);
        }
    }
}
