using System.Net;
using System.Text.Json;

namespace Warranty.Middlewares;

/// <summary>
/// Global exception handling middleware (Buổi 12).
/// Catches unhandled exceptions, logs them, and returns a consistent JSON error body.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment env)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var body = new
            {
                status = 500,
                title = "An unexpected error occurred.",
                detail = env.IsDevelopment() ? ex.Message : "Please try again later or contact support.",
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
        }
    }
}
