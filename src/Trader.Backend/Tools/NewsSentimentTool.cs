using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Scores a headline/paragraph for market sentiment using a lightweight
/// keyword lexicon. Returns a bullish/bearish/neutral tilt and a score.
/// </summary>
public sealed class NewsSentimentTool : ITool
{
    private static readonly HashSet<string> Bullish = new(StringComparer.OrdinalIgnoreCase)
    { "beat", "surge", "rally", "growth", "profit", "upgrade", "bullish", "record", "outperform", "win", "gain", "positive", "breakthrough" };

    private static readonly HashSet<string> Bearish = new(StringComparer.OrdinalIgnoreCase)
    { "miss", "plunge", "crash", "loss", "downgrade", "bearish", "weak", "decline", "layoff", "lawsuit", "drop", "negative", "selloff", "fear" };

    public string Name => "news.sentiment";
    public string Description => "Score a text snippet for market sentiment.";
    public string Parameters => "text";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var text = args.GetValueOrDefault("text");
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(ToolResult.Fail("Provide 'text' to score."));

        var words = text.Split(new[] { ' ', ',', '.', ';', ':', '!', '?', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var bullish = words.Count(w => Bullish.Contains(w));
        var bearish = words.Count(w => Bearish.Contains(w));

        var score = Math.Clamp((bullish - bearish) * 10.0, -100, 100);
        var label = score switch
        {
            > 15 => "bullish",
            < -15 => "bearish",
            _ => "neutral"
        };

        var data = new Dictionary<string, object>
        {
            ["score"] = score,
            ["label"] = label,
            ["bullishHits"] = bullish,
            ["bearishHits"] = bearish,
        };

        var message = $"Sentiment {label} ({score:+0;-0}), {bullish} bullish / {bearish} bearish keywords.";
        return Task.FromResult(ToolResult.Ok(message, data));
    }
}
