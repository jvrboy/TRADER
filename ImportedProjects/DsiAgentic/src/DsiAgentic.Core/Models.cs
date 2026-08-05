using System.Text.Json.Serialization;

namespace DsiAgentic.Core;

public enum Direction { Buy, Sell, Neutral }
public enum SignalStatus { Pending, Active, TpHit, SlHit, Expired, Cancelled }
public enum Regime { PositiveDrift, NegativeDrift, RangeBound, Choppy, HighVolatility, LowVolatility, Unknown }

public sealed record Candle(
    long EpochSec,
    double Open,
    double High,
    double Low,
    double Close,
    double Volume = 0.0);

public sealed class InstrumentConfig
{
    public string Name { get; set; } = "";
    public string Symbol { get; set; } = "";
    public string Family { get; set; } = "";
    public double MinConfluence { get; set; } = 5.0;
    public double AtrTpMult { get; set; } = 2.0;
    public double AtrSlMult { get; set; } = 1.0;
    public int[] Timeframes { get; set; } = new[] { 60, 300, 900 };
}

public sealed class RuntimeConfig
{
    public string ExpiryRule { get; set; } = "TP_OR_SL_ONLY";
    public bool OnePerInstrument { get; set; } = true;
    public int RegimeAgeMaxPct { get; set; } = 60;
    public int CandlesLookback { get; set; } = 200;
    public int PollIntervalMs { get; set; } = 2000;
}

public sealed class RootConfig
{
    public int AppId { get; set; } = 1089;
    public string WsUrl { get; set; } = "wss://ws.derivws.com/websockets/v3?app_id=1089";
    public List<InstrumentConfig> Instruments { get; set; } = new();
    public RuntimeConfig Runtime { get; set; } = new();
}

public sealed class Vote
{
    public string Agent { get; set; } = "";
    public string Family { get; set; } = "";
    public Direction Direction { get; set; } = Direction.Neutral;
    public double Weight { get; set; } = 1.0;
    public double Confidence { get; set; } = 0.5;
    public string Reason { get; set; } = "";
    [JsonIgnore] public Dictionary<string, double> Metrics { get; set; } = new();
}

public sealed class DivergenceEvent
{
    public string Type { get; set; } = ""; // regular_bullish|regular_bearish|hidden_bullish|hidden_bearish
    public string Indicator { get; set; } = "";
    public int TimeframeSec { get; set; }
    public string Grade { get; set; } = "";
    public double Score { get; set; }
    public int BarsAlive { get; set; }
    public double PivotSpan { get; set; }
    public double AngleDelta { get; set; }
}

public sealed class Signal
{
    public string Id { get; set; } = "";
    public string Instrument { get; set; } = "";
    public string Symbol { get; set; } = "";
    public Direction Direction { get; set; }
    public double Entry { get; set; }
    public double TakeProfit { get; set; }
    public double StopLoss { get; set; }
    public double Confluence { get; set; }
    public double BullScore { get; set; }
    public double BearScore { get; set; }
    public int AgentsFired { get; set; }
    public Regime Regime { get; set; }
    public double RegimeAgePct { get; set; }
    public string ExpiryRule { get; set; } = "TP_OR_SL_ONLY";
    public SignalStatus Status { get; set; } = SignalStatus.Pending;
    public double WinProbability { get; set; } = 0.5;
    public long CreatedEpoch { get; set; }
    public long? ClosedEpoch { get; set; }
    public double? ClosedQuote { get; set; }
    public double? PnlPips { get; set; }
    public List<Vote> Votes { get; set; } = new();
    public List<DivergenceEvent> Divergences { get; set; } = new();
    public Dictionary<string, double> Features { get; set; } = new();
}

public sealed class PerformanceRecord
{
    public string Instrument { get; set; } = "";
    public int Total { get; set; }
    public int TpCount { get; set; }
    public int SlCount { get; set; }
    public double WinRate => Total == 0 ? 0.0 : (double)TpCount / Total;
    public double NetPips { get; set; }
    public long LastUpdatedEpoch { get; set; }
}
