namespace AetherBrain.Forex;

public sealed record Candle(DateTimeOffset Time, double Open, double High, double Low, double Close, double Volume);

public enum DivergenceKind { None, RegularBullish, RegularBearish, HiddenBullish, HiddenBearish }

public sealed record DivergenceSignal(
    DivergenceKind Kind,
    string Indicator,
    double Confidence,
    int StartIndex,
    int EndIndex,
    string Evidence);

public sealed record ForexAnalysis(
    string Symbol,
    string Regime,
    double Momentum,
    double Volatility,
    double RiskScore,
    IReadOnlyList<DivergenceSignal> Divergences,
    string Narrative);
