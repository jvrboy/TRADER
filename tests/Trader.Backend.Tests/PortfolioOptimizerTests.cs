using DsiAgentic.Core;
using DsiAgentic.Risk;
using Xunit;

namespace Trader.Backend.Tests;

public class PortfolioOptimizerTests
{
    [Fact]
    public void Optimize_DropsNegativeEdgeCandidates()
    {
        var candidates = new[]
        {
            new AllocationCandidate("EURUSD", Direction.Buy, 0.60, 2.0, 20.0),   // positive edge
            new AllocationCandidate("GBPUSD", Direction.Sell, 0.40, 0.5, -20.0), // negative edge -> dropped
        };

        var result = PortfolioOptimizer.Optimize(candidates, targetTotalAllocation: 1.0);

        Assert.Single(result);
        Assert.Equal("EURUSD", result[0].Symbol);
    }

    [Fact]
    public void Optimize_EmptyInput_ReturnsEmpty()
    {
        var result = PortfolioOptimizer.Optimize(Array.Empty<AllocationCandidate>());
        Assert.Empty(result);
    }

    [Fact]
    public void Optimize_NormalizesToTarget()
    {
        var candidates = new[]
        {
            new AllocationCandidate("A", Direction.Buy, 0.60, 2.0, 20.0),
            new AllocationCandidate("B", Direction.Buy, 0.55, 1.5, 12.0),
        };

        var result = PortfolioOptimizer.Optimize(candidates, targetTotalAllocation: 0.5);

        // Allocations should sum to the target (0.5)
        var sum = result.Sum(r => r.Allocation);
        Assert.Equal(0.5, sum, 2);
        Assert.All(result, r => Assert.InRange(r.Allocation, 0, 0.25));
    }

    [Fact]
    public void Optimize_RespectsMaxKellyCap()
    {
        var candidates = new[]
        {
            new AllocationCandidate("A", Direction.Buy, 0.90, 10.0, 200.0), // huge edge -> capped
        };

        var result = PortfolioOptimizer.Optimize(candidates, targetTotalAllocation: 1.0);

        Assert.Single(result);
        Assert.True(result[0].Allocation <= 0.25);
    }
}
