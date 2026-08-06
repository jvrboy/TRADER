namespace Trader.Backend.Core;

/// <summary>
/// A single tool that an agent can invoke. Tools are self-describing so an
/// agent can discover what is available and call them by name.
/// </summary>
public interface ITool
{
    /// <summary>Stable, unique name used to route calls (e.g. "risk.assess").</summary>
    string Name { get; }

    /// <summary>Short human-readable description for agent discovery.</summary>
    string Description { get; }

    /// <summary>Comma-separated list of parameter names this tool accepts.</summary>
    string Parameters { get; }

    /// <summary>Invoke the tool and return a structured result.</summary>
    Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args);
}

/// <summary>
/// Context passed to every tool invocation. Carries shared state such as the
/// current market snapshot, portfolio, and a logger so tools stay decoupled.
/// </summary>
public sealed class ToolContext
{
    public required DateTimeOffset Now { get; init; }
    public required IReadOnlyList<CandleData> Market { get; init; }
    public Portfolio Portfolio { get; init; } = new();
    public Action<string>? Log { get; init; }

    public double Price(string symbol) =>
        Market.FirstOrDefault(c => c.Symbol == symbol)?.Close ?? 0.0;
}

/// <summary>A lightweight OHLC market record used across tools.</summary>
public sealed record CandleData(
    string Symbol,
    long EpochSec,
    double Open,
    double High,
    double Low,
    double Close,
    double Volume = 0.0);

/// <summary>A simple portfolio used by the portfolio and risk tools.</summary>
public sealed class Portfolio
{
    public List<Position> Positions { get; } = new();

    public double TotalValue => Positions.Sum(p => p.Value);
    public double TotalCost => Positions.Sum(p => p.Cost);
    public double UnrealizedPnl => TotalValue - TotalCost;
}

public sealed record Position(string Symbol, double Quantity, double EntryPrice, double CurrentPrice)
{
    public double Value => Quantity * CurrentPrice;
    public double Cost => Quantity * EntryPrice;
    public double Pnl => Value - Cost;
    public double PnlPct => Cost == 0 ? 0 : (Pnl / Cost) * 100.0;
}

/// <summary>Structured result returned by a tool.</summary>
public sealed record ToolResult(bool Success, string Message, IReadOnlyDictionary<string, object>? Data = null)
{
    public static ToolResult Ok(string message, IReadOnlyDictionary<string, object>? data = null)
        => new(true, message, data);

    public static ToolResult Fail(string message)
        => new(false, message);
}
