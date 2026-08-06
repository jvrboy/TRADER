using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Implements Elliott Wave Oscillator (EWO) analysis to estimate wave cycle phases,
/// identify impulse Wave 3 momentum surges, Wave 4 pullbacks, and Wave 5 divergence.
/// </summary>
public sealed class ElliottWaveTool : ITool
{
    public string Name => "analysis.elliottwave";
    public string Description => "Estimates Elliott Wave phases and divergence using Elliott Wave Oscillator (EWO).";

    public IReadOnlyList<ToolParam> Parameters => new[]
    {
        new ToolParam("symbol", "Ticker symbol to analyze", Required: true),
        new ToolParam("fast", "Fast SMA period (default: 5)", Required: false),
        new ToolParam("slow", "Slow SMA period (default: 35)", Required: false),
    };

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        if (!args.TryGetValue("symbol", out var symbol) || string.IsNullOrWhiteSpace(symbol))
            return Task.FromResult(ToolResult.Fail("Missing required 'symbol' parameter."));

        var fast = int.TryParse(args.GetValueOrDefault("fast"), out var f) && f >= 2 ? f : 5;
        var slow = int.TryParse(args.GetValueOrDefault("slow"), out var s) && s > fast ? s : 35;

        var series = context.GetSeries(symbol);
        if (series.Length < slow + 10)
            return Task.FromResult(ToolResult.Fail($"Insufficient data for Elliott Wave scan. Need at least {slow + 10} candles, got {series.Length}."));

        var closes = series.Select(c => (c.High + c.Low) / 2.0).ToArray();
        var ewo = new double[closes.Length];

        for (var i = slow - 1; i < closes.Length; i++)
        {
            var smaFast = closes[(i - fast + 1)..(i + 1)].Average();
            var smaSlow = closes[(i - slow + 1)..(i + 1)].Average();
            ewo[i] = smaFast - smaSlow;
        }

        var recentEwo = ewo[^Math.Min(40, ewo.Length)..];
        var maxEwo = recentEwo.Max();
        var minEwo = recentEwo.Min();
        var currentEwo = ewo[^1];
        var prevEwo = ewo[^2];

        // Determine current Wave phase
        string phase;
        string action;
        if (currentEwo > 0 && currentEwo >= maxEwo * 0.85)
        {
            phase = "Wave 3 (Strongest Bullish Impulse)";
            action = "Ride trend with trailing stop";
        }
        else if (currentEwo > 0 && currentEwo < maxEwo * 0.40)
        {
            phase = "Wave 4 (Correction / Pullback)";
            action = "Prepare for Wave 5 entry on support";
        }
        else if (currentEwo > 0 && currentEwo < prevEwo && closes[^1] > closes[^10])
        {
            phase = "Wave 5 (Bullish Divergence / Climax)";
            action = "Take profits; expect major ABC correction";
        }
        else if (currentEwo < 0 && currentEwo <= minEwo * 0.85)
        {
            phase = "Wave C / Bearish Impulse";
            action = "Stay short or await bottom confirmation";
        }
        else
        {
            phase = "Consolidation / Wave 1-2 Development";
            action = "Wait for impulse breakout";
        }

        return Task.FromResult(ToolResult.Ok(
            $"{symbol} Elliott Wave: Phase is {phase}. EWO: {currentEwo:+0.0000;-0.0000}. Recommendation: {action}.",
            new Dictionary<string, object>
            {
                ["symbol"] = symbol,
                ["phase"] = phase,
                ["recommendation"] = action,
                ["currentEwo"] = Math.Round(currentEwo, 5),
                ["peakEwo"] = Math.Round(maxEwo, 5),
                ["troughEwo"] = Math.Round(minEwo, 5),
                ["fastPeriod"] = fast,
                ["slowPeriod"] = slow
            }));
    }
}
