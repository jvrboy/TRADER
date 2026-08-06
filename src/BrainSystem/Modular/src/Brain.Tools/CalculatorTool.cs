using System.Data;

namespace Brain.Tools;

/// <summary>
/// Calculator tool: evaluates mathematical expressions safely.
/// Uses DataTable.Compute for expression evaluation.
/// </summary>
public sealed class CalculatorTool : ITool
{
    public string Name => "Calculator";
    public string Description => "Evaluates a mathematical expression and returns the result.";
    public string ParameterSchema => "{\"expression\": \"string\"}";

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("expression", out var exprObj) || exprObj is not string expression)
            return Task.FromResult(ToolResult.Fail("Missing required parameter: expression"));

        try
        {
            var sanitized = expression.Trim();
            if (sanitized.Contains("..") || sanitized.Contains("//"))
                return Task.FromResult(ToolResult.Fail("Invalid expression"));

            var result = new DataTable().Compute(sanitized, null);
            return Task.FromResult(ToolResult.Ok(result.ToString() ?? "0"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail("Calculation error: " + ex.Message));
        }
    }
}
