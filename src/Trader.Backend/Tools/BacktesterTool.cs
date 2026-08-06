using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Runs a simple moving-average crossover backtest over a price series and
/// reports win rate, total return and trade count. Pure computation.
/// </summary>
public sealed class BacktesterTool : ITool
{
    public string Name => "backtest.macross";
    public string Description => "Backtest a fast/slow EMA crossover strategy.";
    public string Parameters => "symbol, fast=9, slow=21, feePct=0.1";

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        var symbol = args.GetValueOrDefault("symbol") ?? context.Market.FirstOrDefault()?.Symbol ?? "?";
        var fast = int.TryParse(args.GetValueOrDefault("fast"), out var f) && f > 1 ? f : 9;
        var slow = int.TryParse(args.GetValueOrDefault("slow"), out var s) && s > fast ? s : 21;
        var feePct = double.TryParse(args.GetValueOrDefault("feePct"), out var fee) ? fee : 0.1;

        var candles = context.Market.Where(c => c.Symbol == symbol).OrderBy(c => c.EpochSec).ToArray();
        if (candles.Length < slow + 5)
            return Task.FromResult(ToolResult.Fail($"Not enough data for '{symbol}' to backtest."));

        var closes = candles.Select(c => c.Close).ToArray();
        var emaFast = Ema(closes, fast);
        var emaSlow = Ema(closes, slow);

        double equity = 1.0;
        bool inMarket = false;
        var trades = 0;
        var wins = 0;
        double entry = 0;

        for (var i = slow; i < closes.Length; i++)
        {
            var crossUp = emaFast[i - 1] <= emaSlow[i - 1] && emaFast[i] > emaSlow[i];
            var crossDown = emaFast[i - 1] >= emaSlow[i - 1] && emaFast[i] < emaSlow[i];

            if (!inMarket && crossUp)
            {
                inMarket = true;
                entry = closes[i];
            }
            else if (inMarket && crossDown)
            {
                inMarket = false;
                trades++;
                var gross = (closes[i] / entry) - 1.0;
                var net = gross - 2 * (feePct / 100.0);
                equity *= 1.0 + net;
                if (net > 0) wins++;
            }
        }

        // close any open trade at the last price
        if (inMarket)
        {
            trades++;
            var net = (closes[^1] / entry) - 1.0 - 2 * (feePct / 100.0);
            equity *= 1.0 + net;
            if (net > 0) wins++;
        }

        var winRate = trades == 0 ? 0 : (double)wins / trades * 100.0;
        var data = new Dictionary<string, object>
        {
            ["symbol"] = symbol,
            ["fast"] = fast,
            ["slow"] = slow,
            ["trades"] = trades,
            ["winRatePct"] = Math.Round(winRate, 1),
            ["totalReturnPct"] = Math.Round((equity - 1.0) * 100.0, 2),
            ["finalEquity"] = Math.Round(equity, 4),
        };

        var message = $"{symbol} {fast}/{slow} EMA cross: {trades} trades, {winRate:0.0}% win rate, {data["totalReturnPct"]}% return.";
        return Task.FromResult(ToolResult.Ok(message, data));
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
