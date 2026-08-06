namespace Brain.Tools;

/// <summary>
/// Unit converter tool: converts between common units.
/// </summary>
public sealed class UnitConverterTool : ITool
{
    public string Name => "UnitConverter";
    public string Description => "Converts between common units (length, weight, temperature, currency-like).";
    public string ParameterSchema => "{\"value\": \"number\", \"from\": \"string\", \"to\": \"string\"}";

    private static readonly Dictionary<string, float> LengthToMeters = new()
    {
        {"m", 1f}, {"km", 1000f}, {"cm", 0.01f}, {"mm", 0.001f},
        {"ft", 0.3048f}, {"in", 0.0254f}, {"mi", 1609.344f}, {"yd", 0.9144f}
    };

    private static readonly Dictionary<string, float> WeightToGrams = new()
    {
        {"g", 1f}, {"kg", 1000f}, {"mg", 0.001f}, {"lb", 453.592f}, {"oz", 28.3495f}
    };

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("value", out var valObj))
            return Task.FromResult(ToolResult.Fail("Missing required parameter: value"));
        if (!parameters.TryGetValue("from", out var fromObj))
            return Task.FromResult(ToolResult.Fail("Missing required parameter: from"));
        if (!parameters.TryGetValue("to", out var toObj))
            return Task.FromResult(ToolResult.Fail("Missing required parameter: to"));

        var value = Convert.ToDouble(valObj);
        var from = fromObj.ToString()!.ToLower();
        var to = toObj.ToString()!.ToLower();

        try
        {
            double result;

            if (from == "c" || from == "f" || from == "k" || to == "c" || to == "f" || to == "k")
            {
                result = ConvertTemperature(value, from, to);
            }
            else if (LengthToMeters.ContainsKey(from) && LengthToMeters.ContainsKey(to))
            {
                var inMeters = value * LengthToMeters[from];
                result = inMeters / LengthToMeters[to];
            }
            else if (WeightToGrams.ContainsKey(from) && WeightToGrams.ContainsKey(to))
            {
                var inGrams = value * WeightToGrams[from];
                result = inGrams / WeightToGrams[to];
            }
            else
            {
                return Task.FromResult(ToolResult.Fail("Unsupported unit conversion: " + from + " to " + to));
            }

            return Task.FromResult(ToolResult.Ok(value + " " + from + " = " + result + " " + to));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail("Conversion error: " + ex.Message));
        }
    }

    private static double ConvertTemperature(double value, string from, string to)
    {
        var celsius = from switch
        {
            "c" => value,
            "f" => (value - 32) * 5 / 9,
            "k" => value - 273.15,
            _ => value
        };

        return to switch
        {
            "c" => celsius,
            "f" => celsius * 9 / 5 + 32,
            "k" => celsius + 273.15,
            _ => celsius
        };
    }
}
