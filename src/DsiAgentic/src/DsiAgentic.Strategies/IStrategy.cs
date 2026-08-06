using DsiAgentic.Core;

namespace DsiAgentic.Strategies;

public interface IStrategy
{
    string Name { get; }
    string Family { get; }
    double Weight { get; }
    Vote? Evaluate(Series s);
}

public sealed class StrategyRegistry
{
    public List<IStrategy> Strategies { get; } = new();
    public StrategyRegistry Register(IStrategy s) { Strategies.Add(s); return this; }
    public List<Vote> RunAll(Series s)
    {
        var res = new List<Vote>();
        foreach (var st in Strategies)
        {
            try { var v = st.Evaluate(s); if (v != null) res.Add(v); }
            catch { /* skip failing strategy */ }
        }
        return res;
    }
}
