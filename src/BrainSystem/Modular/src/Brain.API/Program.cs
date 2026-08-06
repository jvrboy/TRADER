using Brain.API.Middleware;
using Brain.API.Services;
using Brain.LLM;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register orchestration service as singleton
builder.Services.AddSingleton<OrchestrationService>();

// Configure GGUF model path from configuration
var modelPath = builder.Configuration["GGUF:ModelPath"] ?? "models/llm_model.gguf";

var app = builder.Build();

// Load LLM model if available
var orchestrator = app.Services.GetRequiredService<OrchestrationService>();
if (File.Exists(modelPath))
{
    try
    {
        orchestrator.LlmRunner.LoadModel(modelPath);
        app.Logger.LogInformation("GGUF model loaded from {Path}", modelPath);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning("Failed to load GGUF model: {Error}", ex.Message);
    }
}
else
{
    app.Logger.LogWarning("GGUF model not found at {Path}. LLM features will be limited.", modelPath);
}

// Configure middleware
app.UseMiddleware<ApiKeyMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
