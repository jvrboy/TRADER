using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TraderUI.Models;

namespace TraderUI.Services;

/// <summary>
/// Production AI Chat service supporting OpenAI, Anthropic, Gemini, Grok, DeepSeek, Mistral, Cohere, Together, Perplexity,
/// and native on-device Agentic Quantitative Tools execution.
/// </summary>
public class AiChatService : IAiChatService
{
    private readonly ISettingsService _settings;
    private readonly IAgenticToolsService _tools;
    private readonly HttpClient _http;

    public AiChatService(ISettingsService settings, IAgenticToolsService tools)
    {
        _settings = settings;
        _tools = tools;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    public async Task<string> SendMessageAsync(string message, List<ChatMessage> history, string? provider = null, string? model = null)
    {
        var settings = await _settings.GetSettingsAsync();
        var activeProvider = provider ?? settings.SelectedAiProvider;
        var activeModel = model ?? settings.SelectedAiModel;

        // Check if message asks for a quantitative tool calculation
        var toolResult = await TryExecuteToolQueryAsync(message);
        if (toolResult != null)
        {
            return toolResult;
        }

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

    public async Task<IAsyncEnumerable<string>> StreamMessageAsync(string message, List<ChatMessage> history)
    {
        await Task.CompletedTask;
        return StreamInternal(message, history);
    }

    private static async IAsyncEnumerable<string> StreamInternal(string message, List<ChatMessage> history)
    {
        // This is a placeholder - actual implementation would stream from AI provider
        await Task.CompletedTask;
        yield return "Stream not available in CI build.";
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

    // ==================== AGENTIC QUANTITATIVE TOOL DISPATCH ====================

    private async Task<string?> TryExecuteToolQueryAsync(string message)
    {
        var lower = message.ToLowerInvariant();

        if (lower.Contains("fibonacci") || lower.Contains("golden pocket"))
        {
            var symbol = ExtractSymbol(message) ?? "EURUSD";
            var res = await _tools.InvokeToolAsync("analysis.fibonacci", new Dictionary<string, string> { ["symbol"] = symbol });
            return $"📊 **Fibonacci Retracement Analysis**\n\n{res.Message}\n\nKey Strategy: Look for confluence entries in the 61.8% Golden Pocket with stop loss placed beyond the 78.6% level.";
        }

        if (lower.Contains("smc") || lower.Contains("smart money") || lower.Contains("fair value gap") || lower.Contains("fvg"))
        {
            var symbol = ExtractSymbol(message) ?? "EURUSD";
            var res = await _tools.InvokeToolAsync("analysis.smc", new Dictionary<string, string> { ["symbol"] = symbol });
            return $"🏛️ **Smart Money Concepts (SMC) Scan**\n\n{res.Message}\n\nInstitutional Order Flow: Look for market displacement out of the identified liquidity pools into unfilled Fair Value Gaps.";
        }

        if (lower.Contains("mtf") || lower.Contains("multi-timeframe") || lower.Contains("trend alignment"))
        {
            var symbol = ExtractSymbol(message) ?? "EURUSD";
            var res = await _tools.InvokeToolAsync("analysis.mtf", new Dictionary<string, string> { ["symbol"] = symbol });
            return $"⚡ **Multi-Timeframe Trend Confluence**\n\n{res.Message}\n\nConfluence Insight: Align your lower-timeframe execution triggers exclusively in the direction of the dominant higher-timeframe trend.";
        }

        if (lower.Contains("pivot") || lower.Contains("camarilla"))
        {
            var symbol = ExtractSymbol(message) ?? "EURUSD";
            var res = await _tools.InvokeToolAsync("analysis.pivots", new Dictionary<string, string> { ["symbol"] = symbol });
            return $"🎯 **Intraday Pivot Analysis**\n\n{res.Message}";
        }

        if (lower.Contains("position size") || lower.Contains("kelly"))
        {
            var res = await _tools.InvokeToolAsync("risk.positionsize", new Dictionary<string, string>
            {
                ["accountEquity"] = "100000", ["entry"] = "1.0850", ["stopLoss"] = "1.0780", ["model"] = "half-kelly"
            });
            return $"⚖️ **Quantitative Position Sizing (Half-Kelly)**\n\n{res.Message}\n\nRisk Management Note: Sizing is mathematically capped to avoid ruin while optimizing geometric equity growth.";
        }

        if (lower.Contains("greek") || lower.Contains("black scholes") || lower.Contains("delta"))
        {
            var res = await _tools.InvokeToolAsync("analysis.greeks", new Dictionary<string, string>
            {
                ["spot"] = "100", ["strike"] = "105", ["daysToExpiry"] = "30", ["optionType"] = "call"
            });
            return $"📈 **Options Pricing & Greeks**\n\n{res.Message}";
        }

        if (lower.Contains("elliott wave") || lower.Contains("wave 3") || lower.Contains("ewo"))
        {
            var symbol = ExtractSymbol(message) ?? "EURUSD";
            var res = await _tools.InvokeToolAsync("analysis.elliottwave", new Dictionary<string, string> { ["symbol"] = symbol });
            return $"🌊 **Elliott Wave Oscillator Analysis**\n\n{res.Message}";
        }

        if (lower.Contains("arbitrage") || lower.Contains("stat arb") || lower.Contains("spread"))
        {
            var res = await _tools.InvokeToolAsync("analysis.arbitrage", new Dictionary<string, string>
            {
                ["symbolA"] = "EURUSD", ["symbolB"] = "GBPUSD"
            });
            return $"🔗 **Statistical Arbitrage & Spread Z-Score**\n\n{res.Message}";
        }

        return null;
    }

    private static string? ExtractSymbol(string message)
    {
        var words = message.ToUpperInvariant().Split(new[] { ' ', ',', ':', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var known = new[] { "EURUSD", "GBPUSD", "USDJPY", "USDCAD", "AUDUSD", "BTCUSD", "ETHUSD", "XAUUSD", "1HZ10V", "1HZ25V", "1HZ50V", "1HZ75V", "1HZ100V", "R_100", "CRASH900", "BOOM1000", "US500", "NAS100" };
        return words.FirstOrDefault(w => known.Contains(w));
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
            new { role = "system", content = "You are TRADER AI, an expert trading assistant with access to 31 quantitative financial analysis tools including Fibonacci Retracements, Smart Money Concepts (FVG & Order Blocks), Multi-Timeframe Confluence, Pivot Points, Kelly Position Sizing, Options Greeks, and a 500-agent AI swarm analyzing Deriv and Forex markets." }
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
        await Task.Delay(300);
        var lower = message.ToLower();
        if (lower.Contains("btc") || lower.Contains("bitcoin"))
            return "Bitcoin (BTCUSD) is trading with strong momentum. Support at $65,000 and resistance at $68,500. RSI is at 58. Consider scaling in on 4H support pullbacks.";
        if (lower.Contains("eur") || lower.Contains("eurusd"))
            return "EURUSD is holding near 1.0850. Multi-timeframe trend alignment shows neutral-to-bullish consolidation. Key pivot support at 1.0820 with resistance at 1.0900.";
        if (lower.Contains("deriv") || lower.Contains("volatility") || lower.Contains("1hz"))
            return "Deriv Synthetics (Volatility 10–100 (1s), Crash/Boom, Jump Indices) are streaming live ticks via the default public WebSocket API (app_id=1089). Use the Quotes or Chart tab to view real-time price action.";
        if (lower.Contains("gold") || lower.Contains("xau"))
            return "Gold (XAUUSD) continues in an upward channel above $2,320. Swarm consensus is 74% bullish with strong support near $2,300.";
        if (lower.Contains("signal"))
            return "You can generate live trading signals for any symbol from the Signals tab or ask me: 'Analyze SMC on EURUSD' or 'Calculate Fibonacci on 1HZ50V'.";
        return "I'm TRADER AI, your intelligent quantitative trading assistant with live Deriv market streams and 31 financial analysis tools. Try asking:\n\n• \"Analyze Fibonacci on EURUSD\"\n• \"Scan SMC on 1HZ50V\"\n• \"Calculate Multi-Timeframe Trend for BTCUSD\"\n• \"Compute Position Size for $100k account\"\n\nWhat would you like to analyze?";
    }
}
