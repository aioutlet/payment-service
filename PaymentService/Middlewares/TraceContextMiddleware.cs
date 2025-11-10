using PaymentService.Utils;
using System.Text.RegularExpressions;

namespace PaymentService.Middlewares;

/// <summary>
/// Middleware for handling W3C Trace Context (traceparent header)
/// Specification: https://www.w3.org/TR/trace-context/
/// </summary>
public class TraceContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TraceContextMiddleware> _logger;
    private const string TraceparentHeader = "traceparent";
    private const string TraceIdHeader = "X-Trace-ID";
    private const string CorrelationIdHeader = "X-Correlation-ID";

    // W3C Trace Context pattern: 00-{32-hex-trace-id}-{16-hex-span-id}-{2-hex-flags}
    private static readonly Regex TraceparentPattern = new Regex(
        @"^00-([0-9a-f]{32})-([0-9a-f]{16})-[0-9a-f]{2}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public TraceContextMiddleware(RequestDelegate next, ILogger<TraceContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var (traceId, spanId) = ExtractOrGenerateTraceContext(context);

        // Set trace context in HttpContext items
        context.Items["TraceId"] = traceId;
        context.Items["SpanId"] = spanId;
        context.Items["CorrelationId"] = traceId; // Use trace ID as correlation ID

        // Set correlation ID in helper for backward compatibility
        CorrelationIdHelper.SetCorrelationId(traceId);

        // Add W3C traceparent header to response
        var traceparent = $"00-{traceId}-{spanId}-01";
        context.Response.Headers[TraceparentHeader] = traceparent;
        
        // Add trace ID header for easier debugging
        context.Response.Headers[TraceIdHeader] = traceId;
        
        // Support legacy correlation ID header
        context.Response.Headers[CorrelationIdHeader] = traceId;

        _logger.LogDebug("Processing request with trace ID: {TraceId}", traceId.Substring(0, 8));

        try
        {
            await _next(context);
        }
        finally
        {
            _logger.LogDebug("Completed request with trace ID: {TraceId}", traceId.Substring(0, 8));
        }
    }

    private (string traceId, string spanId) ExtractOrGenerateTraceContext(HttpContext context)
    {
        var traceparent = context.Request.Headers[TraceparentHeader].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(traceparent))
        {
            var match = TraceparentPattern.Match(traceparent);
            if (match.Success)
            {
                var traceId = match.Groups[1].Value;
                var spanId = match.Groups[2].Value;

                // Validate trace ID is not all zeros
                if (traceId != "00000000000000000000000000000000" && 
                    spanId != "0000000000000000")
                {
                    return (traceId, spanId);
                }
            }
        }

        // Generate new trace context if extraction failed or no header present
        return GenerateTraceContext();
    }

    private (string traceId, string spanId) GenerateTraceContext()
    {
        // Generate 32-character hex trace ID (128 bits)
        var traceId = GenerateHexString(32);
        
        // Generate 16-character hex span ID (64 bits)
        var spanId = GenerateHexString(16);

        return (traceId, spanId);
    }

    private string GenerateHexString(int length)
    {
        var random = new Random();
        var chars = new char[length];
        
        for (int i = 0; i < length; i++)
        {
            chars[i] = random.Next(16).ToString("x")[0];
        }
        
        return new string(chars);
    }
}

/// <summary>
/// Extension method for registering W3C Trace Context middleware
/// </summary>
public static class TraceContextMiddlewareExtensions
{
    public static IApplicationBuilder UseTraceContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TraceContextMiddleware>();
    }
}
