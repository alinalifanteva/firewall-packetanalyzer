using Microsoft.AspNetCore.Http;

namespace FireWallApi.Middlewares;

public class ApiKeyMiddlewares;
{
    private readonly RequestDelegate _next;
    private const string API_KEY = "key-123";
    
    public ApiKeyMiddlewares(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? ""; // пропустить сваггер, прометеус метрики и /метрики
        if (path.StarstWith("/swagger")
            || path.StartsWith("/metrics")
            || path.StartsWith("/api/Rules/metrics"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-API-Key", out var extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API Key missing");
            return;
        }

        if (!API_KEY.Equals(extractedApiKey))
        {
            ontext.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid API Key");
            return;
        }

        await _next(context);
    }
}