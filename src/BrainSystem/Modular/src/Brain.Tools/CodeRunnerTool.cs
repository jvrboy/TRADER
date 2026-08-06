using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Brain.Tools;

/// <summary>
/// Code runner tool: executes C# scripts safely using Roslyn scripting.
/// Restricted to basic computation - no file system or network access.
/// </summary>
public sealed class CodeRunnerTool : ITool
{
    public string Name => "CodeRunner";
    public string Description => "Executes a C# code snippet and returns the output. Limited to safe operations.";
    public string ParameterSchema => "{\"code\": \"string\"}";

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("code", out var codeObj) || codeObj is not string code)
            return ToolResult.Fail("Missing required parameter: code");

        var forbidden = new[] { "File.", "Directory.", "Process.", "HttpClient", "WebClient", "Socket", "System.IO" };
        foreach (var f in forbidden)
        {
            if (code.Contains(f))
                return ToolResult.Fail("Code contains forbidden operation: " + f);
        }

        try
        {
            var options = ScriptOptions.Default
                .WithImports("System", "System.Math", "System.Linq", "System.Collections.Generic")
                .WithReferences(typeof(System.Math).Assembly);

            var result = await CSharpScript.EvaluateAsync(code, options);
            return ToolResult.Ok(result?.ToString() ?? "null");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail("Code execution error: " + ex.Message);
        }
    }
}
