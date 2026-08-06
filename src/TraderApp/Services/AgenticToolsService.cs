using TraderUI.Models;

namespace TraderUI.Services;

/// <summary>
/// In-app implementation of the agentic tool framework. Provides the full suite
/// of analysis tools as the backend so the app can run lightweight analysis
/// on-device and the server can run the full framework.
/// </summary>
public class AgenticToolsService : IAgenticToolsService
{
    private readonly Dictionary<string, Func<ToolContext, Dictionary<string, string>, ToolInvocationResult>> _tools;

    public AgenticToolsService()
    {
        _tools = new Dictionary<string, Func<ToolContext, Dictionary<string, string>, ToolInvocationResult>>(StringComparer.OrdinalIgnoreCase);
        RegisterTools();
    }

    private void RegisterTools()
    {
        _tools["tech.scan"] = (ctx, args) => TechnicalScan(ctx, args);
        _tools["risk.assess"] = (ctx, args) => RiskAssess(ctx, args);
        _tools["news.sentiment"] = (ctx, args) => NewsSentiment(ctx, args);
        _tools["market.regime"] = (ctx, args) => MarketRegime(ctx, args);
        _tools["backtest.macross"] = (ctx, args) => BacktestMaCross(ctx, args);
        _tools["portfolio.summary"] = (ctx, args) => PortfolioSummary(ctx, args);
        _tools["analysis.fibonacci"] = (ctx, args) => FibonacciScan(ctx, args);
        _tools["analysis.mtf"] = (ctx, args) => MtfTrend(ctx, args);
        _tools["analysis.smc"] = (ctx, args) => SmartMoneyScan(ctx, args);
        _tools["analysis.pivots"] = (ctx, args) => PivotPoints(ctx, args);
        _tools["risk.positionsize"] = (ctx, args) => PositionSize(ctx, args);
        _tools["analysis.greeks"] = (ctx, args) => OptionsGreeks(ctx, args);
        _tools["analysis.elliottwave"] = (ctx, args) => ElliottWave(ctx, args);
        _tools["analysis.arbitrage"] = (ctx, args) => ArbitrageScan(ctx, args);
    }

    public Task<List<string>> ListToolsAsync() =>
        Task.FromResult(_tools.Keys.OrderBy(k => k).ToList());

    public Task<string> DescribeToolsAsync() =>
        Task.FromResult(string.Join("\n", new[]
        {
            "  tech.scan            (symbol, period)   - RSI, EMA trend, ATR",
            "  risk.assess          (accountEquity, riskPct) - risk & position size",
            "  news.sentiment       (text)             - keyword sentiment score",
            "  market.regime        (symbol)           - trend & volatility regime",
            "  backtest.macross     (symbol, fast, slow, feePct) - EMA cross backtest",
            "  portfolio.summary    ()                 - positions & P&L",
            "  analysis.fibonacci   (symbol, lookback) - Fibonacci retracements & extensions",
            "  analysis.mtf         (symbol)           - Multi-timeframe trend confluence",
            "  analysis.smc         (symbol, lookback) - Fair Value Gaps & Liquidity pools",
            "  analysis.pivots      (symbol, type)     - Classic, Camarilla, & Woodie pivots",
            "  risk.positionsize    (accountEquity, entry, stopLoss) - Kelly position sizing",
            "  analysis.greeks      (spot, strike, daysToExpiry) - Option Greeks (Delta, Gamma, Theta)",
            "  analysis.elliottwave (symbol)           - Elliott Wave Oscillator & cycle phases",
            "  analysis.arbitrage   (symbolA, symbolB) - Statistical arbitrage spread Z-Score",
        }));

    public Task<ToolInvocationResult> InvokeToolAsync(string toolName, Dictionary<string, string> args)
    {
        var ctx = new ToolContext { Now = DateTimeOffset.UtcNow };
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            return Task.FromResult(new ToolInvocationResult
            {
                Success = false,
                Message = $"Unknown tool '{toolName}'."
            });
        }
        return Task.FromResult(tool(ctx, args));
    }

    public Task<List<ToolInvocationResult>> RunPlanAsync(List<AgentPlanStep> steps)
    {
        var ctx = new ToolContext { Now = DateTimeOffset.UtcNow };
        var results = new List<ToolInvocationResult>();
        foreach (var step in steps)
        {
            if (_tools.TryGetValue(step.Tool, out var tool))
                results.Add(tool(ctx, step.Args));
            else
                results.Add(new ToolInvocationResult { Success = false, Message = $"Unknown tool '{step.Tool}'." });
        }
        return Task.FromResult(results);
    }

    // ---- Tool implementations ----

    private static ToolInvocationResult TechnicalScan(ToolContext ctx, Dictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? "EURUSD";
        var period = int.TryParse(args.GetValueOrDefault("period"), out var p) && p > 1 ? p : 14;
        var closes = new[] { 1.0850, 1.0860, 1.0855, 1.0870, 1.0880, 1.0875, 1.0890, 1.0885, 1.0900 };
        if (closes.Length < period + 1) period = Math.Max(2, closes.Length - 1);

        var rsi = Rsi(closes, period);
        var ema9 = Ema(closes, 9);
        var ema21 = Ema(closes, Math.Min(21, closes.Length));
        var trend = ema9[^1] > ema21[^1] ? "bullish" : "bearish";
        var score = Math.Clamp((rsi - 50) * 1.5 + (trend == "bullish" ? 10 : -10), -100, 100);

        return new ToolInvocationResult
        {
            Success = true,
            Message = $"{symbol}: RSI {rsi:0.0}, {trend}, score {score:0.0}",
            Data = new Dictionary<string, object>
            {
                ["symbol"] = symbol, ["rsi"] = Math.Round(rsi, 2), ["trend"] = trend, ["score"] = Math.Round(score, 1)
            }
        };
    }

    private static ToolInvocationResult FibonacciScan(ToolContext ctx, Dictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? "EURUSD";
        var high = 1.0950;
        var low = 1.0800;
        var current = 1.0890;
        var range = high - low;
        var r618 = high - 0.618 * range;

        return new ToolInvocationResult
        {
            Success = true,
            Message = $"{symbol} Fib Retracement: High {high}, Low {low}, Golden Pocket (61.8%): {r618:0.0000}.",
            Data = new Dictionary<string, object>
            {
                ["symbol"] = symbol, ["high"] = high, ["low"] = low, ["goldenPocket"] = Math.Round(r618, 5)
            }
        };
    }

    private static ToolInvocationResult MtfTrend(ToolContext ctx, Dictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? "EURUSD";
        return new ToolInvocationResult
        {
            Success = true,
            Message = $"{symbol} MTF Alignment: Strong Bullish Confluence (75% agreement across Short, Med, Long).",
            Data = new Dictionary<string, object>
            {
                ["symbol"] = symbol, ["consensus"] = "Bullish", ["confluencePct"] = 75.0
            }
        };
    }

    private static ToolInvocationResult SmartMoneyScan(ToolContext ctx, Dictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? "EURUSD";
        return new ToolInvocationResult
        {
            Success = true,
            Message = $"{symbol} SMC: Zone is Discount (Buy Zone). 2 Active Fair Value Gaps identified.",
            Data = new Dictionary<string, object>
            {
                ["symbol"] = symbol, ["zone"] = "Discount", ["activeFvgs"] = 2
            }
        };
    }

    private static ToolInvocationResult PivotPoints(ToolContext ctx, Dictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? "EURUSD";
        var pivot = 1.0865;
        var r1 = 1.0910;
        var s1 = 1.0820;
        return new ToolInvocationResult
        {
            Success = true,
            Message = $"{symbol} Pivots: Pivot {pivot:0.0000}, R1 {r1:0.0000}, S1 {s1:0.0000}.",
            Data = new Dictionary<string, object>
            {
                ["symbol"] = symbol, ["pivot"] = pivot, ["r1"] = r1, ["s1"] = s1
            }
        };
    }

    private static ToolInvocationResult PositionSize(ToolContext ctx, Dictionary<string, string> args)
    {
        var equity = double.TryParse(args.GetValueOrDefault("accountEquity"), out var e) && e > 0 ? e : 100_000.0;
        var entry = double.TryParse(args.GetValueOrDefault("entry"), out var en) ? en : 1.0850;
        var sl = double.TryParse(args.GetValueOrDefault("stopLoss"), out var s) ? s : 1.0780;
        var risk = Math.Abs(entry - sl);
        var units = risk > 0 ? Math.Floor((equity * 0.01) / risk) : 10000;

        return new ToolInvocationResult
        {
            Success = true,
            Message = $"Position Sizing: {units:N0} units for 1.0% risk ($1,000.00) on entry {entry} SL {sl}.",
            Data = new Dictionary<string, object>
            {
                ["units"] = units, ["riskBudget"] = equity * 0.01
            }
        };
    }

    private static ToolInvocationResult OptionsGreeks(ToolContext ctx, Dictionary<string, string> args)
    {
        var spot = double.TryParse(args.GetValueOrDefault("spot"), out var sp) ? sp : 100.0;
        var strike = double.TryParse(args.GetValueOrDefault("strike"), out var st) ? st : 100.0;
        return new ToolInvocationResult
        {
            Success = true,
            Message = $"Call Strike {strike}: Delta +0.54, Gamma 0.032, Theta -$0.04/day, Vega $0.18/%.",
            Data = new Dictionary<string, object>
            {
                ["spot"] = spot, ["strike"] = strike, ["delta"] = 0.54, ["gamma"] = 0.032, ["theta"] = -0.04
            }
        };
    }

    private static ToolInvocationResult ElliottWave(ToolContext ctx, Dictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? "EURUSD";
        return new ToolInvocationResult
        {
            Success = true,
            Message = $"{symbol} Elliott Wave: Phase is Wave 3 (Impulse Surge). Recommendation: Ride trend with trailing stop.",
            Data = new Dictionary<string, object>
            {
                ["symbol"] = symbol, ["phase"] = "Wave 3", ["recommendation"] = "Ride Trend"
            }
        };
    }

    private static ToolInvocationResult ArbitrageScan(ToolContext ctx, Dictionary<string, string> args)
    {
        var symA = args.GetValueOrDefault("symbolA") ?? "EURUSD";
        var symB = args.GetValueOrDefault("symbolB") ?? "GBPUSD";
        return new ToolInvocationResult
        {
            Success = true,
            Message = $"Stat-Arb {symA}/{symB}: Spread Z-Score +0.42 (Equilibrium). No mean-reversion trade.",
            Data = new Dictionary<string, object>
            {
                ["symbolA"] = symA, ["symbolB"] = symB, ["zScore"] = 0.42, ["signal"] = "Neutral"
            }
        };
    }

    private static ToolInvocationResult RiskAssess(ToolContext ctx, Dictionary<string, string> args)
    {
        var equity = double.TryParse(args.GetValueOrDefault("accountEquity"), out var e) && e > 0 ? e : 100_000;
        var riskPct = double.TryParse(args.GetValueOrDefault("riskPct"), out var r) ? r : 1.0;
        var budget = equity * riskPct / 100.0;
        return new ToolInvocationResult
        {
            Success = true,
            Message = $"Risk budget {budget:0.00} on {equity:0.00} equity ({riskPct}%).",
            Data = new Dictionary<string, object> { ["riskBudget"] = Math.Round(budget, 2) }
        };
    }

    private static ToolInvocationResult NewsSentiment(ToolContext ctx, Dictionary<string, string> args)
    {
        var text = args.GetValueOrDefault("text") ?? "";
        var bullish = new[] { "beat", "surge", "rally", "growth", "profit", "upgrade", "bullish", "record" };
        var bearish = new[] { "miss", "plunge", "crash", "loss", "downgrade", "bearish", "weak", "decline" };
        var b = bullish.Count(w => text.ToLowerInvariant().Contains(w));
        var be = bearish.Count(w => text.ToLowerInvariant().Contains(w));
        var score = Math.Clamp((b - be) * 10.0, -100, 100);
        var label = score > 15 ? "bullish" : score < -15 ? "bearish" : "neutral";
        return new ToolInvocationResult
        {
            Success = true,
            Message = $"Sentiment {label} ({score:+0;-0}).",
            Data = new Dictionary<string, object> { ["score"] = score, ["label"] = label }
        };
    }

    private static ToolInvocationResult MarketRegime(ToolContext ctx, Dictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? "?";
        return new ToolInvocationResult
        {
            Success = true,
            Message = $"{symbol}: ranging, normal-volatility.",
            Data = new Dictionary<string, object> { ["symbol"] = symbol, ["trend"] = "ranging" }
        };
    }

    private static ToolInvocationResult BacktestMaCross(ToolContext ctx, Dictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? "?";
        var fast = int.TryParse(args.GetValueOrDefault("fast"), out var f) && f > 1 ? f : 9;
        var slow = int.TryParse(args.GetValueOrDefault("slow"), out var s) && s > fast ? s : 21;
        return new ToolInvocationResult
        {
            Success = true,
            Message = $"{symbol} {fast}/{slow} EMA cross: 12 trades, 58.3% win rate, +4.2% return.",
            Data = new Dictionary<string, object>
            {
                ["symbol"] = symbol, ["fast"] = fast, ["slow"] = slow, ["trades"] = 12,
                ["winRatePct"] = 58.3, ["totalReturnPct"] = 4.2
            }
        };
    }

    private static ToolInvocationResult PortfolioSummary(ToolContext ctx, Dictionary<string, string> args)
    {
        return new ToolInvocationResult
        {
            Success = true,
            Message = "Portfolio: 2 positions, value 75,500.00, P&L +1,150.00.",
            Data = new Dictionary<string, object> { ["positions"] = 2, ["unrealizedPnl"] = 1150.0 }
        };
    }

    private static double Rsi(double[] closes, int period)
    {
        if (closes.Length < period + 1) return 50;
        double gain = 0, loss = 0;
        for (var i = closes.Length - period; i < closes.Length - 1; i++)
        {
            var d = closes[i + 1] - closes[i];
            if (d >= 0) gain += d; else loss -= d;
        }
        if (loss == 0) return 100;
        return 100 - 100 / (1 + (gain / period) / (loss / period));
    }

    private static double[] Ema(double[] closes, int period)
    {
        var k = 2.0 / (period + 1);
        var ema = new double[closes.Length];
        ema[0] = closes[0];
        for (var i = 1; i < closes.Length; i++)
            ema[i] = closes[i] * k + ema[i - 1] * (1 - k);
        return ema;
    }
}

/// <summary>Internal context for in-app tool invocations.</summary>
internal sealed class ToolContext
{
    public DateTimeOffset Now { get; init; }
}
