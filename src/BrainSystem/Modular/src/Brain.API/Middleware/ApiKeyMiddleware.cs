using Brain.API.Services;

namespace Brain.API.Middleware;

/// <summary>
/// Simple API key authentication middleware.
/// Reads the key from appsettings.json (ApiKey setting) or environment variable API_KEY.
/// </summary>
public sealed class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _apiKey;
    private readonly bool _authEnabled;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _apiKey = configuration["ApiKey"] ?? Environment.GetEnvironmentVariable("API_KEY") ?? "";
        _authEnabled = !string.IsNullOrEmpty(_apiKey);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_authEnabled || context.Request.Path.StartsWithSegments("/api/status"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-API-Key", out var providedKey) ||
            providedKey != _apiKey)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\": \"Invalid or missing API key\"}");
            return;
        }

        await _next(context);
    }
}
