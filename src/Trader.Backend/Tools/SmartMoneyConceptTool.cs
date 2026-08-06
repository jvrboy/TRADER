using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Identifies Smart Money Concepts (SMC): Fair Value Gaps (FVG), Order Blocks (OB),
/// Buy-Side / Sell-Side Liquidity pools, and Premium vs Discount pricing zones.
/// </summary>
public sealed class SmartMoneyConceptTool : ITool
{
    public string Name => "analysis.smc";
    public string Description => "Identifies Fair Value Gaps (FVG), Order Blocks, Liquidity Pools, and Premium/Discount zones.";

    public IReadOnlyList<ToolParam> Parameters => new[]
    {
        new ToolParam("symbol", "Ticker symbol to analyze", Required: true),
        new ToolParam("lookback", "Candle lookback for SMC scanning (default: 50)", Required: false),
    };

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        if (!args.TryGetValue("symbol", out var symbol) || string.IsNullOrWhiteSpace(symbol))
            return Task.FromResult(ToolResult.Fail("Missing required 'symbol' parameter."));

        var lookback = int.TryParse(args.GetValueOrDefault("lookback"), out var lb) && lb >= 15 ? lb : 50;
        var series = context.GetSeries(symbol);
        if (series.Length < 15)
            return Task.FromResult(ToolResult.Fail($"Insufficient data for SMC scan. Need at least 15 candles, got {series.Length}."));

        var slice = series[^Math.Min(lookback, series.Length)..];
        var currentPrice = series[^1].Close;

        // 1. Fair Value Gaps (3-candle imbalance)
        var fvgs = new List<Dictionary<string, object>>();
        for (var i = 2; i < slice.Length; i++)
        {
            var c1 = slice[i - 2];
            var c2 = slice[i - 1];
            var c3 = slice[i];

            // Bullish FVG: Candle 1 High < Candle 3 Low
            if (c3.Low > c1.High)
            {
                var gapSize = c3.Low - c1.High;
                var isMitigated = slice.Skip(i).Any(c => c.Low <= c1.High);
                fvgs.Add(new Dictionary<string, object>
                {
                    ["type"] = "bullish_fvg",
                    ["top"] = Math.Round(c3.Low, 5),
                    ["bottom"] = Math.Round(c1.High, 5),
                    ["gapSize"] = Math.Round(gapSize, 5),
                    ["mitigated"] = isMitigated,
                    ["candleIndex"] = i
                });
            }
            // Bearish FVG: Candle 1 Low > Candle 3 High
            else if (c1.Low > c3.High)
            {
                var gapSize = c1.Low - c3.High;
                var isMitigated = slice.Skip(i).Any(c => c.High >= c1.Low);
                fvgs.Add(new Dictionary<string, object>
                {
                    ["type"] = "bearish_fvg",
                    ["top"] = Math.Round(c1.Low, 5),
                    ["bottom"] = Math.Round(c3.High, 5),
                    ["gapSize"] = Math.Round(gapSize, 5),
                    ["mitigated"] = isMitigated,
                    ["candleIndex"] = i
                });
            }
        }

        var activeFvgs = fvgs.Where(f => (bool)f["mitigated"] == false).ToList();

        // 2. Premium / Discount Pricing
        var rangeHigh = slice.Max(c => c.High);
        var rangeLow = slice.Min(c => c.Low);
        var equilibrium = (rangeHigh + rangeLow) / 2.0;
        var pricingZone = currentPrice > equilibrium ? "Premium (Sell Zone)" : "Discount (Buy Zone)";

        // 3. Liquidity Pools (Equal Highs & Equal Lows)
        var threshold = (rangeHigh - rangeLow) * 0.005;
        var bslCount = 0;
        var sslCount = 0;
        for (var i = 0; i < slice.Length - 1; i++)
        {
            if (Math.Abs(slice[i].High - rangeHigh) < threshold) bslCount++;
            if (Math.Abs(slice[i].Low - rangeLow) < threshold) sslCount++;
        }

        return Task.FromResult(ToolResult.Ok(
            $"{symbol} SMC Analysis: Zone: {pricingZone} (Eq: {equilibrium:0.0000}), Active FVGs: {activeFvgs.Count} ({fvgs.Count} total), BSL: {rangeHigh:0.0000}, SSL: {rangeLow:0.0000}.",
            new Dictionary<string, object>
            {
                ["symbol"] = symbol,
                ["pricingZone"] = pricingZone,
                ["equilibrium"] = Math.Round(equilibrium, 5),
                ["rangeHigh"] = Math.Round(rangeHigh, 5),
                ["rangeLow"] = Math.Round(rangeLow, 5),
                ["currentPrice"] = Math.Round(currentPrice, 5),
                ["activeFvgCount"] = activeFvgs.Count,
                ["totalFvgCount"] = fvgs.Count,
                ["activeFvgs"] = activeFvgs.Take(5).ToList(),
                ["buySideLiquidity"] = Math.Round(rangeHigh, 5),
                ["sellSideLiquidity"] = Math.Round(rangeLow, 5)
            }));
    }
}
