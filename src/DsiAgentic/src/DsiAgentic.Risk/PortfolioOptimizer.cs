using DsiAgentic.Core;

namespace DsiAgentic.Risk;

/// <summary>
/// A candidate position considered by the optimizer.
/// </summary>
public sealed record AllocationCandidate(
    string Symbol,
    Direction Direction,
    double WinProbability,
    double PayoffRatio,
    double ExpectedReturnPct);

/// <summary>
/// Suggests an optimal allocation across a set of candidate signals using the
/// Kelly criterion (capped) blended with a risk-of-ruin guard, then normalizes
/// to a target total allocation. This is a pure, deterministic computation
/// suitable for agentic portfolio construction.
/// </summary>
public static class PortfolioOptimizer
{
    public const double MaxKellyCap = 0.25;

    /// <summary>
    /// Compute a suggested fractional allocation (0..1 of equity) per candidate.
    /// Candidates with non-positive expected value are dropped.
    /// </summary>
    public static IReadOnlyList<AllocationResult> Optimize(
        IReadOnlyList<AllocationCandidate> candidates,
        double targetTotalAllocation = 1.0)
    {
        if (candidates.Count == 0) return Array.Empty<AllocationResult>();

        var raw = new List<(AllocationCandidate c, double kelly)>();
        foreach (var c in candidates)
        {
            // Kelly: f* = p - (1-p)/b ; b = payoff ratio
            var b = Math.Max(c.PayoffRatio, 1e-9);
            var kelly = c.WinProbability - (1 - c.WinProbability) / b;
            kelly = Math.Clamp(kelly, 0, MaxKellyCap);
            if (kelly <= 0) continue; // negative edge -> skip
            raw.Add((c, kelly));
        }

        if (raw.Count == 0) return Array.Empty<AllocationResult>();

        var total = raw.Sum(r => r.kelly);
        var scale = targetTotalAllocation / total;

        return raw
            .Select(r => new AllocationResult(
                r.c.Symbol,
                r.c.Direction,
                Math.Round(Math.Min(r.kelly * scale, MaxKellyCap), 4),
                Math.Round(r.c.WinProbability, 4),
                Math.Round(r.c.PayoffRatio, 3),
                Math.Round(r.c.ExpectedReturnPct, 3)))
            .OrderByDescending(r => r.Allocation)
            .ToArray();
    }
}

/// <summary>Result of an allocation optimization for one symbol.</summary>
public sealed record AllocationResult(
    string Symbol,
    Direction Direction,
    double Allocation,
    double WinProbability,
    double PayoffRatio,
    double ExpectedReturnPct);
