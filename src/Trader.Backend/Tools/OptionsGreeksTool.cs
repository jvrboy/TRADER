using Trader.Backend.Core;

namespace Trader.Backend.Tools;

/// <summary>
/// Computes European Option Pricing and Greeks (Delta, Gamma, Theta, Vega, Rho) using the Black-Scholes model.
/// </summary>
public sealed class OptionsGreeksTool : ITool
{
    public string Name => "analysis.greeks";
    public string Description => "Calculates Black-Scholes option price and Greeks (Delta, Gamma, Theta, Vega, Rho).";

    public IReadOnlyList<ToolParam> Parameters => new[]
    {
        new ToolParam("spot", "Current underlying spot price", Required: true),
        new ToolParam("strike", "Option strike price", Required: true),
        new ToolParam("daysToExpiry", "Calendar days until expiration", Required: true),
        new ToolParam("volatilityPct", "Implied volatility in percent (default: 20)", Required: false),
        new ToolParam("riskFreeRatePct", "Risk-free interest rate in percent (default: 5.0)", Required: false),
        new ToolParam("optionType", "Option type: 'call' or 'put' (default: 'call')", Required: false),
    };

    public Task<ToolResult> ExecuteAsync(ToolContext context, IReadOnlyDictionary<string, string> args)
    {
        if (!double.TryParse(args.GetValueOrDefault("spot"), out var s) || s <= 0)
            return Task.FromResult(ToolResult.Fail("Invalid or missing 'spot' price."));

        if (!double.TryParse(args.GetValueOrDefault("strike"), out var k) || k <= 0)
            return Task.FromResult(ToolResult.Fail("Invalid or missing 'strike' price."));

        if (!double.TryParse(args.GetValueOrDefault("daysToExpiry"), out var days) || days <= 0)
            return Task.FromResult(ToolResult.Fail("Invalid or missing 'daysToExpiry'."));

        var vol = double.TryParse(args.GetValueOrDefault("volatilityPct"), out var v) && v > 0 ? v / 100.0 : 0.20;
        var rate = double.TryParse(args.GetValueOrDefault("riskFreeRatePct"), out var r) ? r / 100.0 : 0.05;
        var isCall = (args.GetValueOrDefault("optionType") ?? "call").ToLowerInvariant() != "put";

        var t = days / 365.0;
        var sqrtT = Math.Sqrt(t);
        var d1 = (Math.Log(s / k) + (rate + 0.5 * vol * vol) * t) / (vol * sqrtT);
        var d2 = d1 - vol * sqrtT;

        var nd1 = NormalCdf(d1);
        var nd2 = NormalCdf(d2);
        var npd1 = NormalPdf(d1);

        double price, delta, theta, rho;

        if (isCall)
        {
            price = s * nd1 - k * Math.Exp(-rate * t) * nd2;
            delta = nd1;
            theta = (-s * npd1 * vol / (2.0 * sqrtT) - rate * k * Math.Exp(-rate * t) * nd2) / 365.0;
            rho = (k * t * Math.Exp(-rate * t) * nd2) / 100.0;
        }
        else
        {
            price = k * Math.Exp(-rate * t) * NormalCdf(-d2) - s * NormalCdf(-d1);
            delta = nd1 - 1.0;
            theta = (-s * npd1 * vol / (2.0 * sqrtT) + rate * k * Math.Exp(-rate * t) * NormalCdf(-d2)) / 365.0;
            rho = (-k * t * Math.Exp(-rate * t) * NormalCdf(-d2)) / 100.0;
        }

        var gamma = npd1 / (s * vol * sqrtT);
        var vega = (s * sqrtT * npd1) / 100.0; // per 1% change in volatility
        var intrinsic = isCall ? Math.Max(0.0, s - k) : Math.Max(0.0, k - s);
        var timeValue = Math.Max(0.0, price - intrinsic);

        var typeStr = isCall ? "Call" : "Put";
        return Task.FromResult(ToolResult.Ok(
            $"{typeStr} Strike {k} ({days:0}d): Price ${price:0.00} (Delta: {delta:0.000}, Gamma: {gamma:0.0000}, Theta: ${theta:0.00}/d, Vega: ${vega:0.00}/%).",
            new Dictionary<string, object>
            {
                ["optionType"] = typeStr.ToLowerInvariant(),
                ["spot"] = s,
                ["strike"] = k,
                ["daysToExpiry"] = days,
                ["price"] = Math.Round(price, 4),
                ["delta"] = Math.Round(delta, 4),
                ["gamma"] = Math.Round(gamma, 5),
                ["thetaPerDay"] = Math.Round(theta, 4),
                ["vegaPerPct"] = Math.Round(vega, 4),
                ["rhoPerPct"] = Math.Round(rho, 4),
                ["intrinsicValue"] = Math.Round(intrinsic, 4),
                ["timeValue"] = Math.Round(timeValue, 4)
            }));
    }

    private static double NormalCdf(double x)
    {
        // Abramowitz and Stegun approximation
        var b1 = 0.319381530;
        var b2 = -0.356563782;
        var b3 = 1.781477937;
        var b4 = -1.821255978;
        var b5 = 1.330274429;
        var p = 0.2316419;
        var c = 0.39894228;

        if (x >= 0.0)
        {
            var t = 1.0 / (1.0 + p * x);
            return 1.0 - c * Math.Exp(-x * x / 2.0) * t * (t * (t * (t * (t * b5 + b4) + b3) + b2) + b1);
        }
        else
        {
            var t = 1.0 / (1.0 - p * x);
            return c * Math.Exp(-x * x / 2.0) * t * (t * (t * (t * (t * b5 + b4) + b3) + b2) + b1);
        }
    }

    private static double NormalPdf(double x) =>
        (1.0 / Math.Sqrt(2.0 * Math.PI)) * Math.Exp(-0.5 * x * x);
}
