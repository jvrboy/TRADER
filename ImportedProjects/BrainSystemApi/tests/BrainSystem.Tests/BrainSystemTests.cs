using System.Net;
using System.Net.Http.Json;
using BrainSystem.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BrainSystem.Tests;

public sealed class CalculatorToolTests
{
    [Fact]
    public async Task CalculatesSafeArithmetic()
    {
        var tool = new CalculatorTool();
        using var document = System.Text.Json.JsonDocument.Parse("""{"expression":"(5 + 3) * 2"}""");
        var result = await tool.ExecuteAsync(new ToolCall(tool.Name, document.RootElement).Parameters, CancellationToken.None);
        Assert.True(result.Succeeded);
        Assert.Equal("16", result.Output);
    }

    [Fact]
    public async Task RejectsUnsafeExpressions()
    {
        var tool = new CalculatorTool();
        using var document = System.Text.Json.JsonDocument.Parse("""{"expression":"System.IO.File.Delete('x')"}""");
        var result = await tool.ExecuteAsync(document.RootElement, CancellationToken.None);
        Assert.False(result.Succeeded);
    }
}

public sealed class MemoryTests
{
    [Fact]
    public async Task ConsolidatesAndRetrievesRelevantEntry()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<MemoryOptions>(options =>
        {
            options.EpisodicDatabasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        });
        services.AddSingleton<IHierarchicalMemory, HierarchicalMemory>();
        await using var provider = services.BuildServiceProvider();
        var memory = provider.GetRequiredService<IHierarchicalMemory>();
        await memory.RememberAsync(new MemoryEntry(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", "drift index research", "stored", 0.9f), CancellationToken.None);
        await memory.ConsolidateAsync(CancellationToken.None);
        var matches = await memory.QueryAsync("drift index", 10, CancellationToken.None);
        Assert.Single(matches);
    }
}

public sealed class ApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task StatusEndpointReportsReady()
    {
        var response = await factory.CreateClient().GetAsync("/api/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<BrainStatus>();
        Assert.NotNull(payload);
        Assert.True(payload.Ready);
        Assert.True(payload.NetworkCount >= 1000);
    }

    [Fact]
    public async Task PredictEndpointRejectsUnsupportedIndex()
    {
        var response = await factory.CreateClient().GetAsync("/api/predict/99");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}