using Brain.Tools;
using Xunit;

namespace Brain.Tests;

public class ToolTests
{
    [Fact]
    public async Task CalculatorTool_Execute_ReturnsCorrectResult()
    {
        var tool = new CalculatorTool();
        var parameters = new Dictionary<string, object> { { "expression", "2 + 3 * 4" } };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.Equal("14", result.Output);
    }

    [Fact]
    public async Task CalculatorTool_MissingParameter_ReturnsError()
    {
        var tool = new CalculatorTool();
        var parameters = new Dictionary<string, object>();

        var result = await tool.ExecuteAsync(parameters);

        Assert.False(result.Success);
        Assert.Contains("Missing", result.Error);
    }

    [Fact]
    public async Task DateTimeTool_Now_ReturnsCurrentTime()
    {
        var tool = new DateTimeTool();
        var parameters = new Dictionary<string, object> { { "action", "now" } };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.Contains(DateTime.Now.Year.ToString(), result.Output);
    }

    [Fact]
    public async Task UnitConverterTool_ConvertLength_ReturnsCorrectValue()
    {
        var tool = new UnitConverterTool();
        var parameters = new Dictionary<string, object>
        {
            { "value", 1000 },
            { "from", "m" },
            { "to", "km" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.Contains("1", result.Output);
    }

    [Fact]
    public async Task UnitConverterTool_ConvertTemperature_ReturnsCorrectValue()
    {
        var tool = new UnitConverterTool();
        var parameters = new Dictionary<string, object>
        {
            { "value", 0 },
            { "from", "c" },
            { "to", "f" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.Contains("32", result.Output);
    }

    [Fact]
    public void ToolRegistry_RegisterAndGet_ReturnsCorrectTool()
    {
        var registry = new ToolRegistry();
        var tool = new CalculatorTool();
        registry.Register(tool);

        var retrieved = registry.GetTool("Calculator");

        Assert.NotNull(retrieved);
        Assert.Equal("Calculator", retrieved.Name);
    }

    [Fact]
    public async Task ToolRegistry_ExecuteUnknownTool_ReturnsError()
    {
        var registry = new ToolRegistry();

        var result = await registry.ExecuteAsync("NonExistent", new Dictionary<string, object>());

        Assert.False(result.Success);
        Assert.Contains("not found", result.Error);
    }
}
