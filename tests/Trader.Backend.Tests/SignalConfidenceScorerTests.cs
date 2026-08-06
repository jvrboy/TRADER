using DsiAgentic.Core;
using DsiAgentic.Signals;
using Xunit;

namespace Trader.Backend.Tests;

public class SignalConfidenceScorerTests
{
    private static Signal MakeSignal(double confluence = 6.0, double winProb = 0.62, Regime regime = Regime.PositiveDrift)
        => new()
        {
            Id = "t",
            Instrument = "EURUSD",
            Symbol = "EURUSD",
            Direction = Direction.Buy,
            Entry = 1.09,
            Confluence = confluence,
            WinProbability = winProb,
            Regime = regime,
        };

    [Fact]
    public void Score_IsWithinZeroToHundred()
    {
        var s = MakeSignal();
        var score = SignalConfidenceScorer.Score(s);
        Assert.InRange(score, 0, 100);
    }

    [Fact]
    public void Score_HighQuality_Outranks_LowQuality()
    {
        var high = MakeSignal(confluence: 8.0, winProb: 0.75, regime: Regime.PositiveDrift);
        var low = MakeSignal(confluence: 2.0, winProb: 0.51, regime: Regime.Choppy);

        Assert.True(SignalConfidenceScorer.Score(high) > SignalConfidenceScorer.Score(low));
    }

    [Fact]
    public void Grade_MapsScoreRanges()
    {
        Assert.Equal("A", SignalConfidenceScorer.Grade(80));
        Assert.Equal("C", SignalConfidenceScorer.Grade(50));
        Assert.Equal("E", SignalConfidenceScorer.Grade(10));
    }
}
