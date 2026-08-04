using System.Collections.ObjectModel;
using System.Windows.Input;
using TraderUI.Models;
using TraderUI.Services;

namespace TraderUI.ViewModels;

// ==================== BASE VIEW MODEL ====================
public abstract class BaseViewModel : ObservableObject
{
    private bool _isBusy;
    private string _title = "";
    private string _errorMessage = "";
    private bool _hasError;

    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); HasError = !string.IsNullOrEmpty(value); } }
    public bool HasError { get => _hasError; set => SetProperty(ref _hasError, value); }

    protected async Task ExecuteSafeAsync(Func<Task> action)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = "";
            await action();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

// ==================== AI CHAT VIEW MODEL ====================
public class AiChatViewModel : BaseViewModel
{
    private readonly IAiChatService _aiChat;
    private readonly ISettingsService _settings;
    private string _inputText = "";
    private string _selectedProvider = "OpenAI";
    private string _selectedModel = "gpt-4o";
    private bool _isTyping;

    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public ObservableCollection<string> Providers { get; } = new() { "OpenAI", "Anthropic", "Gemini", "Grok", "DeepSeek", "Mistral", "Cohere", "Together", "Perplexity", "Local" };
    public ObservableCollection<string> Models { get; } = new();

    public string InputText { get => _inputText; set => SetProperty(ref _inputText, value); }
    public string SelectedProvider { get => _selectedProvider; set { SetProperty(ref _selectedProvider, value); _ = LoadModelsAsync(); } }
    public string SelectedModel { get => _selectedModel; set => SetProperty(ref _selectedModel, value); }
    public bool IsTyping { get => _isTyping; set => SetProperty(ref _isTyping, value); }

    public ICommand SendCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand QuickActionCommand { get; }

    public AiChatViewModel(IAiChatService aiChat, ISettingsService settings)
    {
        _aiChat = aiChat;
        _settings = settings;
        Title = "AI Chat";
        SendCommand = new Command(async () => await SendMessageAsync(), () => !string.IsNullOrWhiteSpace(InputText) && !IsBusy);
        ClearCommand = new Command(() => { Messages.Clear(); });
        QuickActionCommand = new Command<string>(async (action) => await HandleQuickActionAsync(action));

        // Welcome message
        Messages.Add(new ChatMessage
        {
            Role = MessageRole.Assistant,
            Content = "Welcome to TRADER AI! I'm powered by 500 AI agents and 1,145 technical indicators. Ask me anything about markets, signals, or trading strategies.",
            QuickActions = new List<string> { "Analyze EURUSD", "BTC Signal", "Gold Analysis", "Market Summary" }
        });
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;
        var userMsg = new ChatMessage { Role = MessageRole.User, Content = InputText };
        Messages.Add(userMsg);
        var prompt = InputText;
        InputText = "";
        IsTyping = true;
        var aiMsg = new ChatMessage { Role = MessageRole.Assistant, IsLoading = true };
        Messages.Add(aiMsg);
        try
        {
            var response = await _aiChat.SendMessageAsync(prompt, Messages.ToList(), SelectedProvider, SelectedModel);
            aiMsg.Content = response;
            aiMsg.IsLoading = false;
            aiMsg.QuickActions = GetContextualActions(response);
        }
        catch (Exception ex)
        {
            aiMsg.Content = $"Error: {ex.Message}";
            aiMsg.IsLoading = false;
        }
        finally { IsTyping = false; }
    }

    private async Task HandleQuickActionAsync(string action)
    {
        InputText = action;
        await SendMessageAsync();
    }

    private async Task LoadModelsAsync()
    {
        var models = await _aiChat.GetAvailableModelsAsync(SelectedProvider);
        Models.Clear();
        foreach (var m in models) Models.Add(m);
        if (Models.Count > 0) SelectedModel = Models[0];
    }

    private static List<string> GetContextualActions(string response)
    {
        var actions = new List<string>();
        if (response.Contains("bullish", StringComparison.OrdinalIgnoreCase)) actions.Add("View Chart");
        if (response.Contains("signal", StringComparison.OrdinalIgnoreCase)) actions.Add("Generate Signal");
        if (response.Contains("indicator", StringComparison.OrdinalIgnoreCase)) actions.Add("Run Analysis");
        actions.Add("Save Note");
        return actions.Take(3).ToList();
    }
}

// ==================== QUOTES VIEW MODEL ====================
public class QuotesViewModel : BaseViewModel
{
    private readonly IMarketDataService _marketData;
    private readonly ILocalStorageService _storage;
    private string _searchText = "";
    private string _selectedCategory = "All";
    private Quote? _selectedQuote;

    public ObservableCollection<Quote> Quotes { get; } = new();
    public ObservableCollection<string> Categories { get; } = new() { "All", "Favorites", "Crypto", "Forex", "Indices", "Stocks", "Synthetics" };

    public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); _ = FilterQuotesAsync(); } }
    public string SelectedCategory { get => _selectedCategory; set { SetProperty(ref _selectedCategory, value); _ = FilterQuotesAsync(); } }
    public Quote? SelectedQuote { get => _selectedQuote; set => SetProperty(ref _selectedQuote, value); }

    public ICommand RefreshCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand SelectQuoteCommand { get; }

    private List<Quote> _allQuotes = new();

    public QuotesViewModel(IMarketDataService marketData, ILocalStorageService storage)
    {
        _marketData = marketData;
        _storage = storage;
        Title = "Quotes";
        RefreshCommand = new Command(async () => await LoadQuotesAsync());
        ToggleFavoriteCommand = new Command<Quote>(async (q) => await ToggleFavoriteAsync(q));
        SelectQuoteCommand = new Command<Quote>((q) => SelectedQuote = q);
        _ = LoadQuotesAsync();
    }

    public async Task LoadQuotesAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            _allQuotes = await _marketData.GetQuotesAsync();
            await FilterQuotesAsync();
        });
    }

    private async Task FilterQuotesAsync()
    {
        await Task.CompletedTask;
        var filtered = _allQuotes.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(q => q.Symbol.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || q.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        if (SelectedCategory != "All")
        {
            if (SelectedCategory == "Favorites") filtered = filtered.Where(q => q.IsFavorite);
            else filtered = filtered.Where(q => q.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase));
        }
        Quotes.Clear();
        foreach (var q in filtered) Quotes.Add(q);
    }

    private async Task ToggleFavoriteAsync(Quote quote)
    {
        quote.IsFavorite = !quote.IsFavorite;
        await _storage.SaveAsync("quotes_cache", _allQuotes);
    }
}

// ==================== CHART VIEW MODEL ====================
public class ChartViewModel : BaseViewModel
{
    private readonly IMarketDataService _marketData;
    private readonly IIndicatorService _indicators;
    private readonly IChartAnalysisService _chartAnalysis;
    private string _selectedSymbol = "EURUSD";
    private string _selectedTimeframe = "60";
    private SwarmAnalysisResult? _analysis;
    private bool _showIndicators = true;

    public ObservableCollection<OhlcBar> Bars { get; } = new();
    public ObservableCollection<IndicatorResult> ActiveIndicators { get; } = new();
    public ObservableCollection<string> Symbols { get; } = new() { "EURUSD", "GBPUSD", "USDJPY", "XAUUSD", "BTCUSD", "ETHUSD", "US500", "NAS100", "1HZ50V" };
    public ObservableCollection<Timeframe> Timeframes { get; } = new(Timeframe.All);

    public string SelectedSymbol { get => _selectedSymbol; set { SetProperty(ref _selectedSymbol, value); _ = LoadChartAsync(); } }
    public string SelectedTimeframe { get => _selectedTimeframe; set { SetProperty(ref _selectedTimeframe, value); _ = LoadChartAsync(); } }
    public SwarmAnalysisResult? Analysis { get => _analysis; set => SetProperty(ref _analysis, value); }
    public bool ShowIndicators { get => _showIndicators; set => SetProperty(ref _showIndicators, value); }

    public ICommand RefreshCommand { get; }
    public ICommand AnalyzeCommand { get; }

    public ChartViewModel(IMarketDataService marketData, IIndicatorService indicators, IChartAnalysisService chartAnalysis)
    {
        _marketData = marketData;
        _indicators = indicators;
        _chartAnalysis = chartAnalysis;
        Title = "Chart";
        RefreshCommand = new Command(async () => await LoadChartAsync());
        AnalyzeCommand = new Command(async () => await RunAnalysisAsync());
        _ = LoadChartAsync();
    }

    public async Task LoadChartAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            var bars = await _marketData.GetOhlcAsync(SelectedSymbol, SelectedTimeframe, 200);
            Bars.Clear();
            foreach (var b in bars) Bars.Add(b);
            if (ShowIndicators) await LoadIndicatorsAsync(bars);
        });
    }

    private async Task LoadIndicatorsAsync(List<OhlcBar> bars)
    {
        var results = await _indicators.CalculateAllAsync(SelectedSymbol, SelectedTimeframe, bars);
        ActiveIndicators.Clear();
        foreach (var r in results) ActiveIndicators.Add(r);
    }

    private async Task RunAnalysisAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            var bars = Bars.ToList();
            Analysis = await _chartAnalysis.AnalyzeAsync(SelectedSymbol, SelectedTimeframe, bars);
        });
    }
}

// ==================== SIGNALS VIEW MODEL ====================
public class SignalsViewModel : BaseViewModel
{
    private readonly ISignalService _signalService;
    private string _selectedTab = "Live";
    private string _selectedCategory = "All";

    public ObservableCollection<Signal> LiveSignals { get; } = new();
    public ObservableCollection<Signal> HistorySignals { get; } = new();
    public ObservableCollection<string> Categories { get; } = new() { "All", "Crypto", "Forex", "Indices", "Synthetics" };

    public string SelectedTab { get => _selectedTab; set { SetProperty(ref _selectedTab, value); _ = LoadSignalsAsync(); } }
    public string SelectedCategory { get => _selectedCategory; set { SetProperty(ref _selectedCategory, value); _ = LoadSignalsAsync(); } }

    public ICommand RefreshCommand { get; }
    public ICommand GenerateSignalCommand { get; }
    public ICommand SaveSignalCommand { get; }

    public SignalsViewModel(ISignalService signalService)
    {
        _signalService = signalService;
        Title = "Signals";
        RefreshCommand = new Command(async () => await LoadSignalsAsync());
        GenerateSignalCommand = new Command<string>(async (symbol) => await GenerateSignalAsync(symbol ?? "EURUSD"));
        SaveSignalCommand = new Command<Signal>(async (signal) => await SaveSignalAsync(signal));
        _ = LoadSignalsAsync();
    }

    public async Task LoadSignalsAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            if (SelectedTab == "Live")
            {
                var signals = await _signalService.GetLiveSignalsAsync(SelectedCategory);
                LiveSignals.Clear();
                foreach (var s in signals) LiveSignals.Add(s);
            }
            else
            {
                var history = await _signalService.GetHistoryAsync();
                HistorySignals.Clear();
                foreach (var s in history) HistorySignals.Add(s);
            }
        });
    }

    private async Task GenerateSignalAsync(string symbol)
    {
        await ExecuteSafeAsync(async () =>
        {
            var signal = await _signalService.GenerateSignalAsync(symbol, "60");
            LiveSignals.Insert(0, signal);
            await _signalService.SaveSignalAsync(signal);
        });
    }

    private async Task SaveSignalAsync(Signal signal)
    {
        await _signalService.SaveSignalAsync(signal);
    }
}

// ==================== BOT VIEW MODEL ====================
public class BotViewModel : BaseViewModel
{
    private readonly IBotService _botService;
    private string _selectedTab = "Live";
    private string _selectedBot = "All";
    private BotStats? _stats;
    private bool _isBotRunning;

    public ObservableCollection<BotTrade> OpenTrades { get; } = new();
    public ObservableCollection<BotTrade> ClosedTrades { get; } = new();
    public ObservableCollection<string> BotNames { get; } = new();

    public string SelectedTab { get => _selectedTab; set { SetProperty(ref _selectedTab, value); _ = LoadDataAsync(); } }
    public string SelectedBot { get => _selectedBot; set { SetProperty(ref _selectedBot, value); _ = LoadDataAsync(); } }
    public BotStats? Stats { get => _stats; set => SetProperty(ref _stats, value); }
    public bool IsBotRunning { get => _isBotRunning; set => SetProperty(ref _isBotRunning, value); }

    public ICommand RefreshCommand { get; }
    public ICommand ToggleBotCommand { get; }
    public ICommand CloseTradeCommand { get; }

    public BotViewModel(IBotService botService)
    {
        _botService = botService;
        Title = "Bot";
        RefreshCommand = new Command(async () => await LoadDataAsync());
        ToggleBotCommand = new Command(async () => await ToggleBotAsync());
        CloseTradeCommand = new Command<string>(async (id) => await CloseTradeAsync(id));
        _ = LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            var bots = await _botService.GetBotNamesAsync();
            BotNames.Clear();
            BotNames.Add("All");
            foreach (var b in bots) BotNames.Add(b);

            Stats = await _botService.GetStatsAsync(SelectedBot == "All" ? null : SelectedBot);
            var open = await _botService.GetOpenTradesAsync(SelectedBot == "All" ? null : SelectedBot);
            OpenTrades.Clear();
            foreach (var t in open) OpenTrades.Add(t);

            if (SelectedTab == "History")
            {
                var closed = await _botService.GetTradeHistoryAsync(botName: SelectedBot == "All" ? null : SelectedBot);
                ClosedTrades.Clear();
                foreach (var t in closed) ClosedTrades.Add(t);
            }

            IsBotRunning = await _botService.IsBotRunningAsync(SelectedBot == "All" ? "Swarm Bot v1" : SelectedBot);
        });
    }

    private async Task ToggleBotAsync()
    {
        var botName = SelectedBot == "All" ? "Swarm Bot v1" : SelectedBot;
        if (IsBotRunning) await _botService.StopBotAsync(botName);
        else await _botService.StartBotAsync(botName);
        IsBotRunning = !IsBotRunning;
    }

    private async Task CloseTradeAsync(string tradeId)
    {
        await _botService.CloseTradeAsync(tradeId);
        await LoadDataAsync();
    }
}

// ==================== SETTINGS VIEW MODEL ====================
public class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settings;
    private AppSettings _appSettings = new();

    public AppSettings AppSettings { get => _appSettings; set => SetProperty(ref _appSettings, value); }
    public ObservableCollection<string> Themes { get; } = new() { "Dark", "Light", "Auto" };
    public ObservableCollection<string> ChartStyles { get; } = new() { "Candles", "Bars", "Line", "Area" };
    public ObservableCollection<string> Languages { get; } = new() { "English", "Spanish", "French", "German", "Portuguese", "Arabic", "Chinese", "Japanese" };
    public ObservableCollection<string> AiProviders { get; } = new() { "OpenAI", "Anthropic", "Gemini", "Grok", "DeepSeek", "Mistral", "Cohere", "Together", "Perplexity", "HuggingFace" };

    public ICommand SaveCommand { get; }
    public ICommand ClearDataCommand { get; }

    public SettingsViewModel(ISettingsService settings)
    {
        _settings = settings;
        Title = "Settings";
        SaveCommand = new Command(async () => await SaveSettingsAsync());
        ClearDataCommand = new Command(async () => await ClearDataAsync());
        _ = LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        AppSettings = await _settings.GetSettingsAsync();
    }

    private async Task SaveSettingsAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            await _settings.SaveSettingsAsync(AppSettings);
        });
    }

    private async Task ClearDataAsync()
    {
        // Handled in view with confirmation dialog
    }
}

// ==================== DETAIL VIEW MODELS ====================
public class QuoteDetailViewModel : BaseViewModel
{
    private readonly IMarketDataService _marketData;
    private readonly IChartAnalysisService _chartAnalysis;
    private Quote? _quote;
    private SwarmAnalysisResult? _analysis;

    public Quote? Quote { get => _quote; set => SetProperty(ref _quote, value); }
    public SwarmAnalysisResult? Analysis { get => _analysis; set => SetProperty(ref _analysis, value); }
    public ObservableCollection<OhlcBar> Bars { get; } = new();

    public ICommand LoadCommand { get; }

    public QuoteDetailViewModel(IMarketDataService marketData, IChartAnalysisService chartAnalysis)
    {
        _marketData = marketData;
        _chartAnalysis = chartAnalysis;
        LoadCommand = new Command<string>(async (symbol) => await LoadAsync(symbol));
    }

    public async Task LoadAsync(string symbol)
    {
        await ExecuteSafeAsync(async () =>
        {
            Quote = await _marketData.GetQuoteAsync(symbol);
            var bars = await _marketData.GetOhlcAsync(symbol, "60", 100);
            Bars.Clear();
            foreach (var b in bars) Bars.Add(b);
            Analysis = await _chartAnalysis.AnalyzeAsync(symbol, "60", bars);
        });
    }
}

public class SignalDetailViewModel : BaseViewModel
{
    private Signal? _signal;
    public Signal? Signal { get => _signal; set => SetProperty(ref _signal, value); }
}

public class BotTradeDetailViewModel : BaseViewModel
{
    private BotTrade? _trade;
    public BotTrade? Trade { get => _trade; set => SetProperty(ref _trade, value); }
}
