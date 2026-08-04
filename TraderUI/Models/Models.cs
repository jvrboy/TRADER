using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TraderUI.Models;

// ==================== BASE ====================
public class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}

// ==================== MARKET DATA ====================
public class Quote : ObservableObject
{
    private string _symbol = "";
    private string _name = "";
    private decimal _price;
    private decimal _change;
    private decimal _changePercent;
    private decimal _high;
    private decimal _low;
    private decimal _volume;
    private string _category = "";
    private bool _isFavorite;
    private DateTime _lastUpdated = DateTime.UtcNow;

    public string Symbol { get => _symbol; set => SetProperty(ref _symbol, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public decimal Price { get => _price; set => SetProperty(ref _price, value); }
    public decimal Change { get => _change; set => SetProperty(ref _change, value); }
    public decimal ChangePercent { get => _changePercent; set => SetProperty(ref _changePercent, value); }
    public decimal High { get => _high; set => SetProperty(ref _high, value); }
    public decimal Low { get => _low; set => SetProperty(ref _low, value); }
    public decimal Volume { get => _volume; set => SetProperty(ref _volume, value); }
    public string Category { get => _category; set => SetProperty(ref _category, value); }
    public bool IsFavorite { get => _isFavorite; set => SetProperty(ref _isFavorite, value); }
    public DateTime LastUpdated { get => _lastUpdated; set => SetProperty(ref _lastUpdated, value); }

    public bool IsPositive => Change >= 0;
    public string ChangeDisplay => $"{(Change >= 0 ? "+" : "")}{Change:F4}";
    public string ChangePercentDisplay => $"{(ChangePercent >= 0 ? "+" : "")}{ChangePercent:F2}%";
    public string PriceDisplay => Price.ToString("F5");
}

public class OhlcBar
{
    public DateTime Time { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public bool IsUp => Close >= Open;
}

public class Timeframe
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public static List<Timeframe> All => new()
    {
        new() { Label = "1m", Value = "1" },
        new() { Label = "5m", Value = "5" },
        new() { Label = "15m", Value = "15" },
        new() { Label = "1H", Value = "60" },
        new() { Label = "4H", Value = "240" },
        new() { Label = "1D", Value = "1440" },
        new() { Label = "1W", Value = "10080" },
    };
}

// ==================== SIGNALS ====================
public enum SignalDirection { Buy, Sell, Neutral }
public enum SignalStatus { Live, Won, Lost, Closed, Open }
public enum ConfidenceLevel { Low, Medium, High, VeryHigh }

public class Signal : ObservableObject
{
    private string _id = Guid.NewGuid().ToString();
    private string _symbol = "";
    private SignalDirection _direction;
    private string _timeframe = "";
    private decimal _entryMin;
    private decimal _entryMax;
    private decimal _stopLoss;
    private decimal _takeProfit1;
    private decimal _takeProfit2;
    private decimal _takeProfit3;
    private ConfidenceLevel _confidence;
    private double _confidenceScore;
    private SignalStatus _status;
    private DateTime _generatedAt = DateTime.UtcNow;
    private DateTime? _closedAt;
    private decimal _pnlPercent;
    private decimal _pnlPips;
    private string _category = "";
    private string _strategy = "";
    private string _notes = "";

    public string Id { get => _id; set => SetProperty(ref _id, value); }
    public string Symbol { get => _symbol; set => SetProperty(ref _symbol, value); }
    public SignalDirection Direction { get => _direction; set => SetProperty(ref _direction, value); }
    public string Timeframe { get => _timeframe; set => SetProperty(ref _timeframe, value); }
    public decimal EntryMin { get => _entryMin; set => SetProperty(ref _entryMin, value); }
    public decimal EntryMax { get => _entryMax; set => SetProperty(ref _entryMax, value); }
    public decimal StopLoss { get => _stopLoss; set => SetProperty(ref _stopLoss, value); }
    public decimal TakeProfit1 { get => _takeProfit1; set => SetProperty(ref _takeProfit1, value); }
    public decimal TakeProfit2 { get => _takeProfit2; set => SetProperty(ref _takeProfit2, value); }
    public decimal TakeProfit3 { get => _takeProfit3; set => SetProperty(ref _takeProfit3, value); }
    public ConfidenceLevel Confidence { get => _confidence; set => SetProperty(ref _confidence, value); }
    public double ConfidenceScore { get => _confidenceScore; set => SetProperty(ref _confidenceScore, value); }
    public SignalStatus Status { get => _status; set => SetProperty(ref _status, value); }
    public DateTime GeneratedAt { get => _generatedAt; set => SetProperty(ref _generatedAt, value); }
    public DateTime? ClosedAt { get => _closedAt; set => SetProperty(ref _closedAt, value); }
    public decimal PnlPercent { get => _pnlPercent; set => SetProperty(ref _pnlPercent, value); }
    public decimal PnlPips { get => _pnlPips; set => SetProperty(ref _pnlPips, value); }
    public string Category { get => _category; set => SetProperty(ref _category, value); }
    public string Strategy { get => _strategy; set => SetProperty(ref _strategy, value); }
    public string Notes { get => _notes; set => SetProperty(ref _notes, value); }

    public string DirectionIcon => Direction == SignalDirection.Buy ? "↑" : Direction == SignalDirection.Sell ? "↓" : "→";
    public string DirectionLabel => Direction.ToString().ToUpper();
    public string EntryZone => $"{EntryMin:F5} – {EntryMax:F5}";
    public string ConfidenceDisplay => $"{Confidence} / {ConfidenceScore:F0}%";
    public string TimeAgo => GetTimeAgo(GeneratedAt);
    public bool IsPositive => PnlPercent >= 0;

    private static string GetTimeAgo(DateTime dt)
    {
        var diff = DateTime.UtcNow - dt;
        if (diff.TotalMinutes < 1) return "Just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        return $"{(int)diff.TotalDays}d ago";
    }
}

// ==================== BOT ====================
public enum TradeType { Buy, Sell }
public enum TradeStatus { Open, Closed, Pending }

public class BotTrade : ObservableObject
{
    private string _id = Guid.NewGuid().ToString();
    private string _symbol = "";
    private TradeType _type;
    private decimal _entryPrice;
    private decimal _currentPrice;
    private decimal _exitPrice;
    private decimal _stopLoss;
    private decimal _takeProfit;
    private decimal _lotSize;
    private decimal _riskPercent;
    private decimal _unrealizedPnl;
    private decimal _realizedPnl;
    private decimal _pnlPercent;
    private TradeStatus _status;
    private DateTime _openedAt = DateTime.UtcNow;
    private DateTime? _closedAt;
    private string _botName = "";
    private string _strategy = "";

    public string Id { get => _id; set => SetProperty(ref _id, value); }
    public string Symbol { get => _symbol; set => SetProperty(ref _symbol, value); }
    public TradeType Type { get => _type; set => SetProperty(ref _type, value); }
    public decimal EntryPrice { get => _entryPrice; set => SetProperty(ref _entryPrice, value); }
    public decimal CurrentPrice { get => _currentPrice; set { SetProperty(ref _currentPrice, value); UpdatePnl(); } }
    public decimal ExitPrice { get => _exitPrice; set => SetProperty(ref _exitPrice, value); }
    public decimal StopLoss { get => _stopLoss; set => SetProperty(ref _stopLoss, value); }
    public decimal TakeProfit { get => _takeProfit; set => SetProperty(ref _takeProfit, value); }
    public decimal LotSize { get => _lotSize; set => SetProperty(ref _lotSize, value); }
    public decimal RiskPercent { get => _riskPercent; set => SetProperty(ref _riskPercent, value); }
    public decimal UnrealizedPnl { get => _unrealizedPnl; set => SetProperty(ref _unrealizedPnl, value); }
    public decimal RealizedPnl { get => _realizedPnl; set => SetProperty(ref _realizedPnl, value); }
    public decimal PnlPercent { get => _pnlPercent; set => SetProperty(ref _pnlPercent, value); }
    public TradeStatus Status { get => _status; set => SetProperty(ref _status, value); }
    public DateTime OpenedAt { get => _openedAt; set => SetProperty(ref _openedAt, value); }
    public DateTime? ClosedAt { get => _closedAt; set => SetProperty(ref _closedAt, value); }
    public string BotName { get => _botName; set => SetProperty(ref _botName, value); }
    public string Strategy { get => _strategy; set => SetProperty(ref _strategy, value); }

    public string TypeLabel => Type == TradeType.Buy ? "BUY" : "SELL";
    public bool IsPositive => (Status == TradeStatus.Open ? UnrealizedPnl : RealizedPnl) >= 0;
    public string Duration => GetDuration();

    private void UpdatePnl()
    {
        if (EntryPrice == 0) return;
        var diff = Type == TradeType.Buy ? CurrentPrice - EntryPrice : EntryPrice - CurrentPrice;
        UnrealizedPnl = diff * LotSize * 100000;
        PnlPercent = EntryPrice != 0 ? (diff / EntryPrice) * 100 : 0;
    }

    private string GetDuration()
    {
        var end = ClosedAt ?? DateTime.UtcNow;
        var diff = end - OpenedAt;
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h {diff.Minutes}m";
        return $"{(int)diff.TotalDays}d {diff.Hours}h";
    }
}

public class BotStats
{
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public double WinRate => TotalTrades > 0 ? (double)WinningTrades / TotalTrades * 100 : 0;
    public decimal TotalPnl { get; set; }
    public decimal AverageWin { get; set; }
    public decimal AverageLoss { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal Equity { get; set; }
    public decimal DailyPnl { get; set; }
    public int OpenTrades { get; set; }
}

// ==================== CHAT ====================
public enum MessageRole { User, Assistant, System }

public class ChatMessage : ObservableObject
{
    private string _id = Guid.NewGuid().ToString();
    private MessageRole _role;
    private string _content = "";
    private DateTime _timestamp = DateTime.UtcNow;
    private bool _isLoading;
    private List<string> _quickActions = new();

    public string Id { get => _id; set => SetProperty(ref _id, value); }
    public MessageRole Role { get => _role; set => SetProperty(ref _role, value); }
    public string Content { get => _content; set => SetProperty(ref _content, value); }
    public DateTime Timestamp { get => _timestamp; set => SetProperty(ref _timestamp, value); }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public List<string> QuickActions { get => _quickActions; set => SetProperty(ref _quickActions, value); }

    public bool IsUser => Role == MessageRole.User;
    public bool IsAssistant => Role == MessageRole.Assistant;
    public string TimeDisplay => Timestamp.ToString("HH:mm");
}

// ==================== SETTINGS ====================
public class AppSettings
{
    // AI API Keys
    public string OpenAiApiKey { get; set; } = "";
    public string AnthropicApiKey { get; set; } = "";
    public string GeminiApiKey { get; set; } = "";
    public string GrokApiKey { get; set; } = "";
    public string DeepSeekApiKey { get; set; } = "";
    public string MistralApiKey { get; set; } = "";
    public string CohereApiKey { get; set; } = "";
    public string TogetherApiKey { get; set; } = "";
    public string PerplexityApiKey { get; set; } = "";
    public string HuggingFaceApiKey { get; set; } = "";
    public string SelectedAiProvider { get; set; } = "OpenAI";
    public string SelectedAiModel { get; set; } = "gpt-4o";

    // Broker / Data API Keys
    public string DerivApiKey { get; set; } = "";
    public string AlpacaApiKey { get; set; } = "";
    public string AlpacaSecretKey { get; set; } = "";
    public string BinanceApiKey { get; set; } = "";
    public string BinanceSecretKey { get; set; } = "";
    public string TwelveDataApiKey { get; set; } = "";
    public string AlphaVantageApiKey { get; set; } = "";
    public string PolygonApiKey { get; set; } = "";
    public string FinnhubApiKey { get; set; } = "";
    public string CoinGeckoApiKey { get; set; } = "";

    // Trading Preferences
    public decimal DefaultLeverage { get; set; } = 100;
    public decimal RiskPerTrade { get; set; } = 1.0m;
    public decimal DefaultLotSize { get; set; } = 0.01m;
    public List<string> PreferredAssets { get; set; } = new() { "EURUSD", "BTCUSD", "XAUUSD" };

    // Appearance
    public string Theme { get; set; } = "Dark";
    public string ChartStyle { get; set; } = "Candles";
    public string Language { get; set; } = "English";

    // Notifications
    public bool NotifyNewSignals { get; set; } = true;
    public bool NotifyBotTrades { get; set; } = true;
    public bool NotifyPriceAlerts { get; set; } = true;
    public bool NotifyAiChat { get; set; } = false;
}

// ==================== INDICATORS ====================
public class IndicatorResult
{
    public string Name { get; set; } = "";
    public string Timeframe { get; set; } = "";
    public double Value { get; set; }
    public string Signal { get; set; } = "";
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

public class DivergenceResult
{
    public string Symbol { get; set; } = "";
    public string Timeframe { get; set; } = "";
    public string Type { get; set; } = "";
    public string Indicator { get; set; } = "";
    public string Direction { get; set; } = "";
    public double Strength { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

public class SwarmAnalysisResult
{
    public string Symbol { get; set; } = "";
    public string Timeframe { get; set; } = "";
    public int AgentsUsed { get; set; }
    public int IndicatorsUsed { get; set; }
    public string OverallBias { get; set; } = "";
    public double BullishScore { get; set; }
    public double BearishScore { get; set; }
    public double NeutralScore { get; set; }
    public List<string> KeyLevels { get; set; } = new();
    public List<string> Patterns { get; set; } = new();
    public string Summary { get; set; } = "";
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

// ==================== PRICE ALERT ====================
public class PriceAlert : ObservableObject
{
    private string _id = Guid.NewGuid().ToString();
    private string _symbol = "";
    private decimal _targetPrice;
    private string _condition = "above";
    private bool _isActive = true;
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime? _triggeredAt;

    public string Id { get => _id; set => SetProperty(ref _id, value); }
    public string Symbol { get => _symbol; set => SetProperty(ref _symbol, value); }
    public decimal TargetPrice { get => _targetPrice; set => SetProperty(ref _targetPrice, value); }
    public string Condition { get => _condition; set => SetProperty(ref _condition, value); }
    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }
    public DateTime CreatedAt { get => _createdAt; set => SetProperty(ref _createdAt, value); }
    public DateTime? TriggeredAt { get => _triggeredAt; set => SetProperty(ref _triggeredAt, value); }
}
