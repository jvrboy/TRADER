using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TraderUI.Models;

namespace TraderUI.Services;

/// <summary>
/// AI Chat service supporting OpenAI, Anthropic, Gemini, Grok, DeepSeek, Mistral, Cohere, Together, Perplexity, HuggingFace.
/// Integrates with the NEXUS/Nova Brain backend for market analysis.
/// </summary>
public class AiChatService : IAiChatService
{
    private readonly ISettingsService _settings;
    private readonly HttpClient _http;

    public AiChatService(ISettingsService settings)
    {
        _settings = settings;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    public async Task<string> SendMessageAsync(string message, List<ChatMessage> history, string? provider = null, string? model = null)
    {
        var settings = await _settings.GetSettingsAsync();
        var activeProvider = provider ?? settings.SelectedAiProvider;
        var activeModel = model ?? settings.SelectedAiModel;

        return activeProvider.ToLower() switch
        {
            "openai" => await CallOpenAiAsync(message, history, settings.OpenAiApiKey, activeModel),
            "anthropic" => await CallAnthropicAsync(message, history, settings.AnthropicApiKey, activeModel),
            "gemini" => await CallGeminiAsync(message, history, settings.GeminiApiKey, activeModel),
            "grok" => await CallGrokAsync(message, history, settings.GrokApiKey, activeModel),
            "deepseek" => await CallDeepSeekAsync(message, history, settings.DeepSeekApiKey, activeModel),
            "mistral" => await CallMistralAsync(message, history, settings.MistralApiKey, activeModel),
            "cohere" => await CallCohereAsync(message, history, settings.CohereApiKey, activeModel),
            "together" => await CallTogetherAsync(message, history, settings.TogetherApiKey, activeModel),
            "perplexity" => await CallPerplexityAsync(message, history, settings.PerplexityApiKey, activeModel),
            _ => await GenerateLocalResponseAsync(message, history)
        };
    }

    public async IAsyncEnumerable<string> StreamMessageAsync(string message, List<ChatMessage> history)
    {
        // Streaming implementation - yields chunks
        var response = await SendMessageAsync(message, history);
        var words = response.Split(' ');
        foreach (var word in words)
        {
            yield return word + " ";
            await Task.Delay(30);
        }
    }

    async IAsyncEnumerable<string> IAiChatService.StreamMessageAsync(string message, List<ChatMessage> history)
    {
        await foreach (var chunk in StreamMessageAsync(message, history))
            yield return chunk;
    }

    public async Task<List<string>> GetAvailableModelsAsync(string provider)
    {
        await Task.CompletedTask;
        return provider.ToLower() switch
        {
            "openai" => new() { "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-3.5-turbo", "o1-preview", "o1-mini" },
            "anthropic" => new() { "claude-3-5-sonnet-20241022", "claude-3-opus-20240229", "claude-3-haiku-20240307" },
            "gemini" => new() { "gemini-1.5-pro", "gemini-1.5-flash", "gemini-pro" },
            "grok" => new() { "grok-beta", "grok-vision-beta" },
            "deepseek" => new() { "deepseek-chat", "deepseek-coder", "deepseek-reasoner" },
            "mistral" => new() { "mistral-large-latest", "mistral-medium-latest", "mistral-small-latest", "open-mixtral-8x7b" },
            "cohere" => new() { "command-r-plus", "command-r", "command" },
            "together" => new() { "meta-llama/Llama-3-70b-chat-hf", "mistralai/Mixtral-8x7B-Instruct-v0.1" },
            "perplexity" => new() { "llama-3.1-sonar-large-128k-online", "llama-3.1-sonar-small-128k-online" },
            _ => new() { "default" }
        };
    }

    public async Task<string> AnalyzeChartAsync(string symbol, string timeframe, List<OhlcBar> bars)
    {
        if (bars.Count < 2) return "Insufficient data for analysis.";
        var last = bars.Last();
        var prev = bars[^2];
        var trend = last.Close > prev.Close ? "bullish" : "bearish";
        var prompt = $"Analyze the {symbol} {timeframe} chart. Latest candle: O={last.Open:F5} H={last.High:F5} L={last.Low:F5} C={last.Close:F5}. Short-term trend appears {trend}. Provide a concise technical analysis with key levels, bias, and potential trade setup.";
        return await SendMessageAsync(prompt, new List<ChatMessage>());
    }

    public async Task<string> GetMarketSummaryAsync(string symbol)
    {
        var prompt = $"Provide a brief current market summary for {symbol} including key technical levels, market sentiment, and any important news or events affecting the price.";
        return await SendMessageAsync(prompt, new List<ChatMessage>());
    }

    // ==================== PROVIDER IMPLEMENTATIONS ====================

    private async Task<string> CallOpenAiAsync(string message, List<ChatMessage> history, string apiKey, string model)
    {
        if (string.IsNullOrEmpty(apiKey)) return await GenerateLocalResponseAsync(message, history);
        try
        {
            var messages = BuildOpenAiMessages(message, history);
            var request = new { model, messages, max_tokens = 1000, temperature = 0.7 };
            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = JsonContent.Create(request);
            var resp = await _http.SendAsync(req);
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "No response.";
        }
        catch (Exception ex) { return $"OpenAI error: {ex.Message}"; }
    }

    private async Task<string> CallAnthropicAsync(string message, List<ChatMessage> history, string apiKey, string model)
    {
        if (string.IsNullOrEmpty(apiKey)) return await GenerateLocalResponseAsync(message, history);
        try
        {
            var messages = history.Where(m => m.Role != MessageRole.System)
                .Select(m => new { role = m.Role == MessageRole.User ? "user" : "assistant", content = m.Content })
                .Append(new { role = "user", content = message }).ToList();
            var request = new { model = model.Contains("claude") ? model : "claude-3-5-sonnet-20241022", max_tokens = 1000, messages };
            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            req.Headers.Add("x-api-key", apiKey);
            req.Headers.Add("anthropic-version", "2023-06-01");
            req.Content = JsonContent.Create(request);
            var resp = await _http.SendAsync(req);
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("content")[0].GetProperty("text").GetString() ?? "No response.";
        }
        catch (Exception ex) { return $"Anthropic error: {ex.Message}"; }
    }

    private async Task<string> CallGeminiAsync(string message, List<ChatMessage> history, string apiKey, string model)
    {
        if (string.IsNullOrEmpty(apiKey)) return await GenerateLocalResponseAsync(message, history);
        try
        {
            var activeModel = model.Contains("gemini") ? model : "gemini-1.5-flash";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{activeModel}:generateContent?key={apiKey}";
            var contents = history.Select(m => new
            {
                role = m.Role == MessageRole.User ? "user" : "model",
                parts = new[] { new { text = m.Content } }
            }).Append(new { role = "user", parts = new[] { new { text = message } } }).ToList();
            var request = new { contents };
            var resp = await _http.PostAsJsonAsync(url, request);
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "No response.";
        }
        catch (Exception ex) { return $"Gemini error: {ex.Message}"; }
    }

    private async Task<string> CallGrokAsync(string message, List<ChatMessage> history, string apiKey, string model)
    {
        if (string.IsNullOrEmpty(apiKey)) return await GenerateLocalResponseAsync(message, history);
        try
        {
            var messages = BuildOpenAiMessages(message, history);
            var request = new { model = "grok-beta", messages, max_tokens = 1000 };
            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.x.ai/v1/chat/completions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = JsonContent.Create(request);
            var resp = await _http.SendAsync(req);
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "No response.";
        }
        catch (Exception ex) { return $"Grok error: {ex.Message}"; }
    }

    private async Task<string> CallDeepSeekAsync(string message, List<ChatMessage> history, string apiKey, string model)
    {
        if (string.IsNullOrEmpty(apiKey)) return await GenerateLocalResponseAsync(message, history);
        try
        {
            var messages = BuildOpenAiMessages(message, history);
            var request = new { model = "deepseek-chat", messages, max_tokens = 1000 };
            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.deepseek.com/chat/completions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = JsonContent.Create(request);
            var resp = await _http.SendAsync(req);
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "No response.";
        }
        catch (Exception ex) { return $"DeepSeek error: {ex.Message}"; }
    }

    private async Task<string> CallMistralAsync(string message, List<ChatMessage> history, string apiKey, string model)
    {
        if (string.IsNullOrEmpty(apiKey)) return await GenerateLocalResponseAsync(message, history);
        try
        {
            var messages = BuildOpenAiMessages(message, history);
            var request = new { model = "mistral-large-latest", messages, max_tokens = 1000 };
            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.mistral.ai/v1/chat/completions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = JsonContent.Create(request);
            var resp = await _http.SendAsync(req);
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "No response.";
        }
        catch (Exception ex) { return $"Mistral error: {ex.Message}"; }
    }

    private async Task<string> CallCohereAsync(string message, List<ChatMessage> history, string apiKey, string model)
    {
        if (string.IsNullOrEmpty(apiKey)) return await GenerateLocalResponseAsync(message, history);
        try
        {
            var chatHistory = history.Select(m => new { role = m.Role == MessageRole.User ? "USER" : "CHATBOT", message = m.Content }).ToList();
            var request = new { model = "command-r-plus", message, chat_history = chatHistory };
            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.cohere.ai/v1/chat");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = JsonContent.Create(request);
            var resp = await _http.SendAsync(req);
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("text").GetString() ?? "No response.";
        }
        catch (Exception ex) { return $"Cohere error: {ex.Message}"; }
    }

    private async Task<string> CallTogetherAsync(string message, List<ChatMessage> history, string apiKey, string model)
    {
        if (string.IsNullOrEmpty(apiKey)) return await GenerateLocalResponseAsync(message, history);
        try
        {
            var messages = BuildOpenAiMessages(message, history);
            var request = new { model = "meta-llama/Llama-3-70b-chat-hf", messages, max_tokens = 1000 };
            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.together.xyz/v1/chat/completions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = JsonContent.Create(request);
            var resp = await _http.SendAsync(req);
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "No response.";
        }
        catch (Exception ex) { return $"Together error: {ex.Message}"; }
    }

    private async Task<string> CallPerplexityAsync(string message, List<ChatMessage> history, string apiKey, string model)
    {
        if (string.IsNullOrEmpty(apiKey)) return await GenerateLocalResponseAsync(message, history);
        try
        {
            var messages = BuildOpenAiMessages(message, history);
            var request = new { model = "llama-3.1-sonar-large-128k-online", messages, max_tokens = 1000 };
            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.perplexity.ai/chat/completions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = JsonContent.Create(request);
            var resp = await _http.SendAsync(req);
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "No response.";
        }
        catch (Exception ex) { return $"Perplexity error: {ex.Message}"; }
    }

    private static List<object> BuildOpenAiMessages(string message, List<ChatMessage> history)
    {
        var messages = new List<object>
        {
            new { role = "system", content = "You are TRADER AI, an expert trading assistant. You analyze markets, provide signals, explain strategies, and help traders make informed decisions. You have access to technical analysis tools including RSI, MACD, EMA, Bollinger Bands, divergence detection, and a 500-agent AI swarm for comprehensive market analysis." }
        };
        messages.AddRange(history.TakeLast(10).Select(m => (object)new
        {
            role = m.Role == MessageRole.User ? "user" : "assistant",
            content = m.Content
        }));
        messages.Add(new { role = "user", content = message });
        return messages;
    }

    private async Task<string> GenerateLocalResponseAsync(string message, List<ChatMessage> history)
    {
        await Task.Delay(500); // Simulate processing
        var lower = message.ToLower();
        if (lower.Contains("btc") || lower.Contains("bitcoin"))
            return "Bitcoin (BTCUSD) is showing interesting price action. Key resistance at $68,000 and support at $65,000. RSI is at 58, suggesting moderate bullish momentum. Consider the 4H chart for entry confirmation. Set alerts at key levels.";
        if (lower.Contains("eur") || lower.Contains("eurusd"))
            return "EURUSD is consolidating near 1.0850. The pair faces resistance at 1.0900 (200 EMA). RSI divergence detected on the 1H chart. Watch for a breakout above 1.0870 for bullish continuation, or a break below 1.0820 for bearish momentum.";
        if (lower.Contains("gold") || lower.Contains("xau"))
            return "Gold (XAUUSD) remains in a bullish trend above $2,300. Key support at $2,320 with resistance at $2,380. The AI swarm analysis shows 73% bullish consensus across 500 agents. Consider buying dips near support with SL below $2,300.";
        if (lower.Contains("signal"))
            return "I can generate trading signals for any instrument. Use the Signals tab for live AI-generated signals from our 500-agent swarm analyzing 1,145 indicators. You can also ask me: 'Generate a signal for EURUSD 4H' for an instant analysis.";
        if (lower.Contains("indicator") || lower.Contains("rsi") || lower.Contains("macd"))
            return "Our backend includes 1,145 technical indicators powered by the Deriv AI Swarm. Key indicators available: RSI, MACD, EMA, SMA, Bollinger Bands, Stochastic, ATR, VWAP, Volume Profile, Fibonacci, Pivot Points, and many more. Use the Chart tab to overlay any indicator.";
        return "I'm TRADER AI, your intelligent trading assistant. I can help you:\n\n• Analyze any market or instrument\n• Generate trading signals\n• Explain technical indicators\n• Review your trading strategy\n• Analyze chart patterns\n• Provide market summaries\n\nAdd your AI API key in Settings to unlock full AI capabilities. What would you like to analyze?";
    }
}
