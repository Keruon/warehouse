using Serilog.Context;

namespace Storage.Middleware;

public class RequestLoggingMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue)
            ? headerValue.ToString()
            : Guid.NewGuid().ToString();

        context.Response.Headers[CorrelationIdHeader] = correlationId;

        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var startedAt = DateTime.UtcNow;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("UserId", userId))
        {
            await _next(context);
        }

        var elapsedMs = (DateTime.UtcNow - startedAt).TotalMilliseconds;
        _logger.LogInformation(
            "HTTP {Method} {Path} responded {StatusCode} in {DurationMs}ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            Math.Round(elapsedMs, 2));
    }
}
