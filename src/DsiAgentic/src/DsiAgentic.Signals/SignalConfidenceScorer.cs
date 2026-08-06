using DsiAgentic.Core;

namespace DsiAgentic.Signals;

/// <summary>
/// Computes a 0-100 confidence score for a generated signal by blending
/// confluence strength, win probability, and regime quality. Pure and
/// deterministic so it can be unit-tested and used by agents.
/// </summary>
public static class SignalConfidenceScorer
{
    /// <summary>
    /// Score a signal on a 0-100 scale. Higher is more confident.
    /// </summary>
    public static double Score(Signal s)
    {
        // Confluence: stronger vote spread -> higher score (0..40)
        var confluence = Math.Clamp(s.Confluence / 8.0, 0, 1) * 40.0;

        // Win probability: how far above coin-flip (0..40)
        var edge = Math.Clamp((s.WinProbability - 0.5) / 0.25, 0, 1) * 40.0;

        // Regime quality: trending regimes are more tradeable (0..20)
        var regime = s.Regime switch
        {
            Regime.PositiveDrift or Regime.NegativeDrift => 1.0,
            Regime.HighVolatility or Regime.LowVolatility => 0.6,
            Regime.RangeBound => 0.4,
            _ => 0.2,
        } * 20.0;

        return Math.Round(Math.Clamp(confluence + edge + regime, 0, 100), 1);
    }

    /// <summary>Human-readable grade for a score.</summary>
    public static string Grade(double score) => score switch
    {
        >= 75 => "A",
        >= 60 => "B",
        >= 45 => "C",
        >= 30 => "D",
        _ => "E",
    };
}
