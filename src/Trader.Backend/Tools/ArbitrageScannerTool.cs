using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Scans pair spreads, calculates statistical arbitrage Z-scores, and identifies
/// mean-reversion cointegration trade opportunities between correlated assets.
/// </summary>
public sealed class ArbitrageScannerTool : ITool
{
    public string Name => "analysis.arbitrage";
    public string Description => "Calculates statistical pair spread Z-Score for mean-reversion arbitrage.";

    public IReadOnlyList<ToolParam> Parameters => new[]
    {
        new ToolParam("symbolA", "Primary asset symbol", Required: true),
        new ToolParam("symbolB", "Secondary asset symbol", Required: true),
        new ToolParam("lookback", "Lookback period for rolling mean and std (default: 60)", Required: false),
    };

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        if (!args.TryGetValue("symbolA", out var symbolA) || string.IsNullOrWhiteSpace(symbolA))
            return Task.FromResult(ToolResult.Fail("Missing required 'symbolA' parameter."));

        if (!args.TryGetValue("symbolB", out var symbolB) || string.IsNullOrWhiteSpace(symbolB))
            return Task.FromResult(ToolResult.Fail("Missing required 'symbolB' parameter."));

        var lookback = int.TryParse(args.GetValueOrDefault("lookback"), out var lb) && lb >= 10 ? lb : 60;

        var seriesA = context.GetSeries(symbolA);
        var seriesB = context.GetSeries(symbolB);

        var count = Math.Min(seriesA.Length, seriesB.Length);
        if (count < 15)
            return Task.FromResult(ToolResult.Fail($"Insufficient data for pair {symbolA}/{symbolB}. Need at least 15 matched candles."));

        var n = Math.Min(lookback, count);
        var aCloses = seriesA[^n..].Select(c => c.Close).ToArray();
        var bCloses = seriesB[^n..].Select(c => c.Close).ToArray();

        // Calculate simple beta hedge ratio
        var meanA = aCloses.Average();
        var meanB = bCloses.Average();
        var cov = 0.0;
        var varB = 0.0;
        for (var i = 0; i < n; i++)
        {
            cov += (aCloses[i] - meanA) * (bCloses[i] - meanB);
            varB += (bCloses[i] - meanB) * (bCloses[i] - meanB);
        }
        var beta = varB > 0 ? cov / varB : 1.0;

        // Calculate spread series: Spread = PriceA - beta * PriceB
        var spreads = new double[n];
        for (var i = 0; i < n; i++)
            spreads[i] = aCloses[i] - beta * bCloses[i];

        var spreadMean = spreads.Average();
        var spreadStd = Math.Sqrt(spreads.Select(s => (s - spreadMean) * (s - spreadMean)).Average());
        if (spreadStd <= 1e-8) spreadStd = 1.0;

        var currentSpread = spreads[^1];
        var zScore = (currentSpread - spreadMean) / spreadStd;

        string signal;
        if (zScore >= 2.0) signal = $"Short {symbolA} / Long {symbolB} (Spread overextended high)";
        else if (zScore <= -2.0) signal = $"Long {symbolA} / Short {symbolB} (Spread overextended low)";
        else signal = "Neutral / Equilibrium (No arbitrage trade)";

        return Task.FromResult(ToolResult.Ok(
            $"Stat-Arb {symbolA}/{symbolB}: Spread Z-Score {zScore:+0.00;-0.00} (Beta: {beta:0.000}). Signal: {signal}.",
            new Dictionary<string, object>
            {
                ["symbolA"] = symbolA,
                ["symbolB"] = symbolB,
                ["hedgeRatioBeta"] = Math.Round(beta, 4),
                ["currentSpread"] = Math.Round(currentSpread, 5),
                ["spreadMean"] = Math.Round(spreadMean, 5),
                ["spreadStd"] = Math.Round(spreadStd, 5),
                ["zScore"] = Math.Round(zScore, 3),
                ["signal"] = signal
            }));
    }
}
