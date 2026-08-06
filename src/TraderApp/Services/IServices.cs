using TraderUI.Models;

namespace TraderUI.Services;

// ==================== LOCAL STORAGE ====================
public interface ILocalStorageService
{
    Task SaveAsync<T>(string key, T value);
    Task<T?> LoadAsync<T>(string key);
    Task DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task ClearAllAsync();
}

// ==================== SETTINGS ====================
public interface ISettingsService
{
    Task<AppSettings> GetSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
    Task<string> GetApiKeyAsync(string provider);
    Task SetApiKeyAsync(string provider, string key);
}

// ==================== MARKET DATA ====================
public interface IMarketDataService
{
    Task<List<Quote>> GetQuotesAsync(string category = "all");
    Task<Quote?> GetQuoteAsync(string symbol);
    Task<List<OhlcBar>> GetOhlcAsync(string symbol, string timeframe, int count = 200);
    Task<List<Quote>> SearchQuotesAsync(string query);
    Task SubscribeToQuoteAsync(string symbol, Action<Quote> onUpdate);
    Task UnsubscribeFromQuoteAsync(string symbol);
    Task<List<string>> GetAvailableSymbolsAsync();
}

// ==================== AI CHAT ====================
public interface IAiChatService
{
    Task<string> SendMessageAsync(string message, List<ChatMessage> history, string? provider = null, string? model = null);
    Task<IAsyncEnumerable<string>> StreamMessageAsync(string message, List<ChatMessage> history);
    Task<List<string>> GetAvailableModelsAsync(string provider);
    Task<string> AnalyzeChartAsync(string symbol, string timeframe, List<OhlcBar> bars);
    Task<string> GetMarketSummaryAsync(string symbol);
}

// ==================== SIGNALS ====================
public interface ISignalService
{
    Task<List<Signal>> GetLiveSignalsAsync(string category = "all");
    Task<List<Signal>> GetHistoryAsync(DateTime? from = null, DateTime? to = null, string? status = null);
    Task<Signal?> GetSignalAsync(string id);
    Task SaveSignalAsync(Signal signal);
    Task DeleteSignalAsync(string id);
    Task<Signal> GenerateSignalAsync(string symbol, string timeframe);
    Task<List<Signal>> RunSwarmAnalysisAsync(string symbol, string timeframe);
}

// ==================== BOT ====================
public interface IBotService
{
    Task<List<BotTrade>> GetOpenTradesAsync(string? botName = null);
    Task<List<BotTrade>> GetTradeHistoryAsync(DateTime? from = null, DateTime? to = null, string? botName = null);
    Task<BotStats> GetStatsAsync(string? botName = null);
    Task<BotTrade> OpenTradeAsync(string symbol, TradeType type, decimal lotSize, decimal sl, decimal tp, string botName = "Manual");
    Task CloseTradeAsync(string tradeId);
    Task UpdateSlTpAsync(string tradeId, decimal sl, decimal tp);
    Task<bool> IsBotRunningAsync(string botName);
    Task StartBotAsync(string botName);
    Task StopBotAsync(string botName);
    Task<List<string>> GetBotNamesAsync();
}

// ==================== NOTIFICATIONS ====================
public interface INotificationService
{
    Task RequestPermissionAsync();
    Task ShowNotificationAsync(string title, string body, string? data = null);
    Task ScheduleAlertAsync(PriceAlert alert);
    Task CancelAlertAsync(string alertId);
}

// ==================== INDICATOR SERVICE ====================
public interface IIndicatorService
{
    Task<List<IndicatorResult>> CalculateAllAsync(string symbol, string timeframe, List<OhlcBar> bars);
    Task<double> CalculateRsiAsync(List<OhlcBar> bars, int period = 14);
    Task<(double macd, double signal, double hist)> CalculateMacdAsync(List<OhlcBar> bars, int fast = 12, int slow = 26, int signal = 9);
    Task<double> CalculateEmaAsync(List<OhlcBar> bars, int period);
    Task<double> CalculateSmaAsync(List<OhlcBar> bars, int period);
    Task<double> CalculateAtrAsync(List<OhlcBar> bars, int period = 14);
    Task<(double upper, double middle, double lower)> CalculateBollingerAsync(List<OhlcBar> bars, int period = 20, double stdDev = 2.0);
    Task<(double k, double d)> CalculateStochasticAsync(List<OhlcBar> bars, int kPeriod = 14, int dPeriod = 3);
}

// ==================== DIVERGENCE SERVICE ====================
public interface IDivergenceService
{
    Task<List<DivergenceResult>> DetectDivergencesAsync(string symbol, string timeframe, List<OhlcBar> bars);
    Task<DivergenceResult?> GetLatestDivergenceAsync(string symbol);
}

// ==================== DRIFT LAB SERVICE ====================
public interface IDriftLabService
{
    Task<string> GetSignalAsync(string strategy, List<OhlcBar> bars, double threshold = 0.5);
    Task<Dictionary<string, double>> CompareStrategiesAsync(List<OhlcBar> bars);
    Task<Dictionary<string, object>> BacktestAsync(string strategy, List<OhlcBar> bars);
}

// ==================== SYNTHETICS SERVICE ====================
public interface ISyntheticsService
{
    Task<List<OhlcBar>> GenerateSyntheticOhlcAsync(string index, int periods = 500, double volatility = 0.02);
    Task<Dictionary<string, object>> BacktestStrategyAsync(List<OhlcBar> bars, string strategyName);
    Task<List<string>> GetAvailableIndicesAsync();
}

// ==================== AI BRAIN SERVICE ====================
public interface IAiBrainService
{
    Task<double> PredictAsync(string taskName, double[] inputs);
    Task TrainAsync(string taskName, List<(double[] inputs, double[] outputs)> examples, int epochs = 200);
    Task<Dictionary<string, object>> GetStatsAsync(string taskName);
    Task<List<string>> GetTaskNamesAsync();
}

// ==================== CHART ANALYSIS SERVICE ====================
public interface IChartAnalysisService
{
    Task<SwarmAnalysisResult> AnalyzeAsync(string symbol, string timeframe, List<OhlcBar> bars);
    Task<List<Dictionary<string, object>>> DetectOrderBlocksAsync(List<OhlcBar> bars);
    Task<List<Dictionary<string, object>>> DetectFairValueGapsAsync(List<OhlcBar> bars);
    Task<double> GetConfluenceScoreAsync(List<OhlcBar> bars);
    Task<string> GetAiSummaryAsync(string symbol, string timeframe, List<OhlcBar> bars);
}

// ==================== SWARM ANALYSIS SERVICE ====================
public interface ISwarmAnalysisService
{
    Task<SwarmAnalysisResult> RunSwarmAsync(string symbol, string timeframe, List<OhlcBar> bars);
    Task<List<Signal>> GenerateSignalsFromSwarmAsync(string symbol, string timeframe, List<OhlcBar> bars);
    Task<int> GetAgentCountAsync();
    Task<int> GetIndicatorCountAsync();
}

// ==================== DERIV API SERVICE ====================
public interface IDerivApiService
{
    Task ConnectAsync(string apiKey);
    Task DisconnectAsync();
    Task<bool> IsConnectedAsync();
    Task<List<Quote>> GetTicksAsync(List<string> symbols);
    Task<List<OhlcBar>> GetCandlesAsync(string symbol, string granularity, int count);
    Task<Dictionary<string, object>> GetAccountInfoAsync();
    Task<BotTrade> BuyContractAsync(string symbol, string contractType, decimal amount, int duration);
    Task SellContractAsync(string contractId);
}

// ==================== AGENTIC TOOLS SERVICE ====================
/// <summary>
/// Exposes the agentic tool framework to the app: a registry of analysis
/// tools, agent discovery, and tool invocation. Mirrors the backend's
/// tool framework so the same tools run in-app and on the server.
/// </summary>
public interface IAgenticToolsService
{
    /// <summary>List the names of every registered tool.</summary>
    Task<List<string>> ListToolsAsync();

    /// <summary>Describe every registered tool (name, params, description).</summary>
    Task<string> DescribeToolsAsync();

    /// <summary>Invoke a tool by name with string arguments.</summary>
    Task<ToolInvocationResult> InvokeToolAsync(string toolName, Dictionary<string, string> args);

    /// <summary>Run a scripted multi-tool agent plan.</summary>
    Task<List<ToolInvocationResult>> RunPlanAsync(List<AgentPlanStep> steps);
}

/// <summary>Result of a single tool invocation in the app.</summary>
public sealed class ToolInvocationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public Dictionary<string, object>? Data { get; set; }
}

/// <summary>One step in an agent plan: tool name + arguments.</summary>
public sealed class AgentPlanStep
{
    public string Tool { get; set; } = "";
    public Dictionary<string, string> Args { get; set; } = new();
}
