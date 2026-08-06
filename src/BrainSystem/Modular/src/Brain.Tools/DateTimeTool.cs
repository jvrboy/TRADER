namespace Brain.Tools;

/// <summary>
/// Date/time tool: returns current date, time, and performs date calculations.
/// </summary>
public sealed class DateTimeTool : ITool
{
    public string Name => "DateTime";
    public string Description => "Returns the current date and time, or performs date calculations.";
    public string ParameterSchema => "{\"action\": \"string\", \"date\": \"string?\"}";

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var action = parameters.TryGetValue("action", out var a) ? a.ToString() : "now";

        try
        {
            var result = action switch
            {
                "now" => DateTime.Now.ToString("O"),
                "utc" => DateTime.UtcNow.ToString("O"),
                "date" => DateTime.Now.ToString("yyyy-MM-dd"),
                "time" => DateTime.Now.ToString("HH:mm:ss"),
                "dayofweek" => DateTime.Now.DayOfWeek.ToString(),
                _ => DateTime.Now.ToString("O")
            };
            return Task.FromResult(ToolResult.Ok(result));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail("DateTime error: " + ex.Message));
        }
    }
}
