using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// A declarative strategy builder. Lets a caller describe a trading rule as a
/// set of indicator conditions joined by AND/OR logic, then evaluates it
/// against the market and returns whether it fires (long / short / flat).
/// This is a lightweight, composable rule engine for strategies.
/// </summary>
public sealed class StrategyBuilderTool : ITool
{
    public string Name => "strategy.evaluate";
    public string Description => "Evaluate a declarative indicator rule against the market.";
    public string Parameters => "symbol, rule";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? context.Market.FirstOrDefault()?.Symbol ?? "?";
        var rule = args.GetValueOrDefault("rule") ?? "";
        if (string.IsNullOrWhiteSpace(rule))
            return Task.FromResult(ToolResult.Fail("Provide a 'rule' expression."));

        var closes = context.Market.Where(c => c.Symbol == symbol).OrderBy(c => c.EpochSec).Select(c => c.Close).ToArray();
        if (closes.Length < 40)
            return Task.FromResult(ToolResult.Fail($"Not enough data for '{symbol}'."));

        // Compute a small set of indicators used by rules.
        var rsi = Rsi(closes, 14);
        var ema9 = Ema(closes, 9);
        var ema21 = Ema(closes, 21);
        var ma20 = closes[^20..].Average();

        // Supported conditions: rsi_gt, rsi_lt, price_gt_ema9, price_lt_ema9,
        // ema9_gt_ema21, ema9_lt_ema21, price_gt_ma20, price_lt_ma20.
        // Rules are space-separated tokens with AND/OR, e.g.
        // "rsi_lt_30 AND price_gt_ema9 OR ema9_gt_ema21"
        var tokens = rule.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var lastPrice = closes[^1];

        bool EvaluateCondition(string cond) => cond.ToLowerInvariant() switch
        {
            "rsi_gt_50" => rsi > 50,
            "rsi_lt_50" => rsi < 50,
            "rsi_gt_70" => rsi > 70,
            "rsi_lt_30" => rsi < 30,
            "price_gt_ema9" => lastPrice > ema9[^1],
            "price_lt_ema9" => lastPrice < ema9[^1],
            "ema9_gt_ema21" => ema9[^1] > ema21[^1],
            "ema9_lt_ema21" => ema9[^1] < ema21[^1],
            "price_gt_ma20" => lastPrice > ma20,
            "price_lt_ma20" => lastPrice < ma20,
            _ => throw new FormatException($"Unknown condition '{cond}'")
        };

        // Simple left-to-right AND/OR evaluation.
        try
        {
            var result = EvaluateTokens(tokens, EvaluateCondition);
            var direction = result ? (lastPrice > ema9[^1] ? "long" : "short") : "flat";

            var data = new Dictionary<string, object>
            {
                ["symbol"] = symbol,
                ["rule"] = rule,
                ["fires"] = result,
                ["direction"] = direction,
                ["rsi"] = Math.Round(rsi, 2),
                ["ema9"] = Math.Round(ema9[^1], 4),
                ["ema21"] = Math.Round(ema21[^1], 4),
            };

            var message = result
                ? $"{symbol}: rule fires -> {direction}."
                : $"{symbol}: rule does not fire.";
            return Task.FromResult(ToolResult.Ok(message, data));
        }
        catch (FormatException ex)
        {
            return Task.FromResult(ToolResult.Fail(ex.Message));
        }
    }

    private static bool EvaluateTokens(string[] tokens, Func<string, bool> eval)
    {
        var result = false;
        var pendingOp = "OR";
        var first = true;
        foreach (var t in tokens)
        {
            if (t.Equals("AND", StringComparison.OrdinalIgnoreCase) || t.Equals("OR", StringComparison.OrdinalIgnoreCase))
            {
                pendingOp = t.ToUpperInvariant();
                continue;
            }
            var val = eval(t);
            if (first) { result = val; first = false; }
            else if (pendingOp == "AND") result = result && val;
            else result = result || val;
        }
        return result;
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
