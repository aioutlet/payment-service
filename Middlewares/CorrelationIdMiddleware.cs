using PaymentService.Utils;

namespace PaymentService.Middlewares;

/// <summary>
/// Middleware for handling correlation IDs in HTTP requests
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract correlation ID from header or generate new one
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault() 
                          ?? Guid.NewGuid().ToString();

        // Set correlation ID in context
        CorrelationIdHelper.SetCorrelationId(correlationId);

        // Add correlation ID to response headers
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        // Add correlation ID to HttpContext items for easy access
        context.Items["CorrelationId"] = correlationId;

        _logger.LogDebug("Processing request with correlation ID: {CorrelationId}", correlationId);

        try
        {
            await _next(context);
        }
        finally
        {
            _logger.LogDebug("Completed request with correlation ID: {CorrelationId}", correlationId);
        }
    }
}

/// <summary>
/// Extension method for registering correlation ID middleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
