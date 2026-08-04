using TraderUI.Models;

namespace TraderUI.Services;

public class SettingsService : ISettingsService
{
    private const string SettingsKey = "app_settings";
    private readonly ILocalStorageService _storage;
    private AppSettings? _cached;

    public SettingsService(ILocalStorageService storage)
    {
        _storage = storage;
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        if (_cached != null) return _cached;
        _cached = await _storage.LoadAsync<AppSettings>(SettingsKey) ?? new AppSettings();
        return _cached;
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        _cached = settings;
        await _storage.SaveAsync(SettingsKey, settings);
    }

    public async Task<string> GetApiKeyAsync(string provider)
    {
        var settings = await GetSettingsAsync();
        return provider.ToLower() switch
        {
            "openai" => settings.OpenAiApiKey,
            "anthropic" => settings.AnthropicApiKey,
            "gemini" => settings.GeminiApiKey,
            "grok" => settings.GrokApiKey,
            "deepseek" => settings.DeepSeekApiKey,
            "mistral" => settings.MistralApiKey,
            "cohere" => settings.CohereApiKey,
            "together" => settings.TogetherApiKey,
            "perplexity" => settings.PerplexityApiKey,
            "huggingface" => settings.HuggingFaceApiKey,
            "deriv" => settings.DerivApiKey,
            "alpaca" => settings.AlpacaApiKey,
            "binance" => settings.BinanceApiKey,
            "twelvedata" => settings.TwelveDataApiKey,
            "alphavantage" => settings.AlphaVantageApiKey,
            "polygon" => settings.PolygonApiKey,
            "finnhub" => settings.FinnhubApiKey,
            "coingecko" => settings.CoinGeckoApiKey,
            _ => ""
        };
    }

    public async Task SetApiKeyAsync(string provider, string key)
    {
        var settings = await GetSettingsAsync();
        switch (provider.ToLower())
        {
            case "openai": settings.OpenAiApiKey = key; break;
            case "anthropic": settings.AnthropicApiKey = key; break;
            case "gemini": settings.GeminiApiKey = key; break;
            case "grok": settings.GrokApiKey = key; break;
            case "deepseek": settings.DeepSeekApiKey = key; break;
            case "mistral": settings.MistralApiKey = key; break;
            case "cohere": settings.CohereApiKey = key; break;
            case "together": settings.TogetherApiKey = key; break;
            case "perplexity": settings.PerplexityApiKey = key; break;
            case "huggingface": settings.HuggingFaceApiKey = key; break;
            case "deriv": settings.DerivApiKey = key; break;
            case "alpaca": settings.AlpacaApiKey = key; break;
            case "binance": settings.BinanceApiKey = key; break;
            case "twelvedata": settings.TwelveDataApiKey = key; break;
            case "alphavantage": settings.AlphaVantageApiKey = key; break;
            case "polygon": settings.PolygonApiKey = key; break;
            case "finnhub": settings.FinnhubApiKey = key; break;
            case "coingecko": settings.CoinGeckoApiKey = key; break;
        }
        await SaveSettingsAsync(settings);
    }
}
