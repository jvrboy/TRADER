using BrainSystem.Core;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddBrainSystemCore(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("public-api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 60;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.AutoReplenishment = true;
    });
});

var app = builder.Build();
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    app.Logger.LogError(feature?.Error, "Unhandled API exception.");
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
}));
app.UseHttpsRedirection();
app.UseRateLimiter();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var api = app.MapGroup("/api").RequireRateLimiting("public-api");
api.MapGet("/status", (IBrainEnsemble ensemble, IHierarchicalMemory memory, IGgufLlmProvider llm) =>
    Results.Ok(new BrainStatus(true, ensemble.Count, llm.IsConfigured, memory.ShortTermCount, memory.LongTermCount, DateTimeOffset.UtcNow)));

api.MapPost("/train", (TrainingRequest request, HttpRequest httpRequest, IOptions<SecurityOptions> securityOptions, IBrainEnsemble ensemble, IHierarchicalMemory memory, CancellationToken cancellationToken) =>
{
    var requiredKey = securityOptions.Value.ApiKey;
    if (!string.IsNullOrWhiteSpace(requiredKey) && !string.Equals(httpRequest.Headers["X-Api-Key"], requiredKey, StringComparison.Ordinal))
    {
        return Results.Unauthorized();
    }

    var result = ensemble.Train(request, cancellationToken);
    _ = memory.RememberAsync(new MemoryEntry(Guid.NewGuid(), DateTimeOffset.UtcNow, "training", "train", result.Notice, 0.9f), cancellationToken);
    return Results.Ok(result);
});

api.MapGet("/predict/{index:int}", async (int index, IMarketDataSource source, IBrainEnsemble ensemble, IHierarchicalMemory memory, CancellationToken cancellationToken) =>
{
    if (index is not (10 or 20 or 30))
    {
        return Results.BadRequest(new { error = "Only indices 10, 20, and 30 are supported." });
    }
    var ticks = await source.GetHistoryAsync(index, 64, cancellationToken);
    var result = ensemble.Predict(index, ticks);
    await memory.RememberAsync(new MemoryEntry(Guid.NewGuid(), DateTimeOffset.UtcNow, "prediction", $"index:{index}", result.Direction, 0.7f), cancellationToken);
    return Results.Ok(result);
});

api.MapPost("/chat", async (ChatRequest request, AgentService agent, CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await agent.ChatAsync(request, cancellationToken));
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

api.MapGet("/memory/query", async (string q, int? limit, IHierarchicalMemory memory, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(q))
    {
        return Results.BadRequest(new { error = "Query parameter q is required." });
    }
    return Results.Ok(await memory.QueryAsync(q, limit ?? 10, cancellationToken));
});

app.MapHealthChecks("/healthz", new HealthCheckOptions { ResponseWriter = async (context, report) =>
{
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new { status = report.Status.ToString().ToLowerInvariant() });
}});

app.Run();
public partial class Program { }