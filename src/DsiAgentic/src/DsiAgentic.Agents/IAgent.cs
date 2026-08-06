using DsiAgentic.Core;

namespace DsiAgentic.Agents;

public interface IAgent
{
    string Name { get; }
    string Family { get; }
    double Weight { get; }
    IEnumerable<Vote> Evaluate(Dictionary<int, Series> mtf);
}
