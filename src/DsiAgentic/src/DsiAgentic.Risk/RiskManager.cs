using DsiAgentic.Core;
using DsiAgentic.Indicators;

namespace DsiAgentic.Risk;

public static class RiskManager
{
    public static (double tp, double sl) ComputeTpSl(Series s, Direction dir, double atrTpMult, double atrSlMult)
    {
        var atr = SeriesMath.Atr(s.High, s.Low, s.Close, 14);
        var a = atr[^1]; if (double.IsNaN(a) || a <= 0) a = Math.Abs(s.Close[^1]) * 0.001;
        var entry = s.Close[^1];
        return dir == Direction.Buy
            ? (entry + atrTpMult * a, entry - atrSlMult * a)
            : (entry - atrTpMult * a, entry + atrSlMult * a);
    }

    public static double KellyFraction(double winRate, double payoff)
    {
        if (payoff <= 0) return 0;
        var k = winRate - (1 - winRate) / payoff;
        return Math.Max(0, Math.Min(0.25, k));
    }

    public static double MonteCarloWinProb(Series s, Direction dir, double tp, double sl, int paths = 500, int horizon = 40)
    {
        var rng = new Random(42);
        int n = s.Close.Length; if (n < 30) return 0.5;
        var rets = new double[n - 1];
        for (int i = 1; i < n; i++) rets[i - 1] = Math.Log(s.Close[i] / s.Close[i - 1]);
        int wins = 0;
        double entry = s.Close[^1];
        for (int p = 0; p < paths; p++)
        {
            double px = entry;
            for (int h = 0; h < horizon; h++)
            {
                px *= Math.Exp(rets[rng.Next(rets.Length)]);
                if (dir == Direction.Buy)
                {
                    if (px >= tp) { wins++; break; }
                    if (px <= sl) break;
                }
                else
                {
                    if (px <= tp) { wins++; break; }
                    if (px >= sl) break;
                }
            }
        }
        return (double)wins / paths;
    }
}
