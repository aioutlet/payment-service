using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace PaymentService.Middlewares;

/// <summary>
/// Rate limiting configuration and middleware for Payment Service
/// Implements .NET 8 built-in rate limiting capabilities with payment-specific policies
/// </summary>
public static class RateLimitingConfiguration
{
    public const string GeneralPolicy = "GeneralPolicy";
    public const string PaymentProcessingPolicy = "PaymentProcessingPolicy";
    public const string PaymentMethodManagementPolicy = "PaymentMethodManagementPolicy";
    public const string RefundProcessingPolicy = "RefundProcessingPolicy";
    public const string TransactionHistoryPolicy = "TransactionHistoryPolicy";
    public const string AdminPolicy = "AdminPolicy";

    /// <summary>
    /// Configure rate limiting services with payment-specific policies
    /// </summary>
    public static IServiceCollection AddRateLimitingServices(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitConfig = configuration.GetSection("RateLimiting");
        var isEnabled = rateLimitConfig.GetValue<bool>("Enabled", false); // Default to false for safety
        
        // Also check if we're in test environment
        var environment = configuration.GetValue<string>("ENVIRONMENT") ?? 
                         configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") ?? 
                         Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? 
                         "Production";
        
        // Disable rate limiting in test environments
        if (environment.Equals("Test", StringComparison.OrdinalIgnoreCase) || 
            environment.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            isEnabled = false;
        }

        if (!isEnabled)
        {
            // Add a no-op rate limiter if disabled
            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                    PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    {
                        return RateLimitPartition.GetNoLimiter<string>("disabled");
                    })
                );
            });
            return services;
        }

        var windowSizeInMinutes = rateLimitConfig.GetValue<int>("WindowSizeInMinutes", 15);
        var requestLimit = rateLimitConfig.GetValue<int>("RequestLimit", 1000);

        services.AddRateLimiter(options =>
        {
            // General API endpoints - moderate
            options.AddPolicy(GeneralPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = requestLimit,
                        Window = TimeSpan.FromMinutes(windowSizeInMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    }));

            // Payment processing - very restrictive due to financial sensitivity
            options.AddPolicy(PaymentProcessingPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(requestLimit / 20, 10), // Very restrictive for payments
                        Window = TimeSpan.FromMinutes(windowSizeInMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 3
                    }));

            // Payment method management - restrictive
            options.AddPolicy(PaymentMethodManagementPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(requestLimit / 10, 20), // Restrictive for sensitive operations
                        Window = TimeSpan.FromMinutes(windowSizeInMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    }));

            // Refund processing - highly restrictive
            options.AddPolicy(RefundProcessingPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(requestLimit / 50, 5), // Very restrictive for refunds
                        Window = TimeSpan.FromMinutes(windowSizeInMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2
                    }));

            // Transaction history - lenient (read operations)
            options.AddPolicy(TransactionHistoryPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = requestLimit * 2, // More lenient for read operations
                        Window = TimeSpan.FromMinutes(windowSizeInMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 20
                    }));

            // Admin operations - restrictive
            options.AddPolicy(AdminPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(requestLimit / 20, 15), // Restrictive for admin operations
                        Window = TimeSpan.FromMinutes(windowSizeInMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 3
                    }));

            // Global limiter as fallback
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: GetPartitionKey(httpContext),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = requestLimit * 2, // Higher global limit
                            Window = TimeSpan.FromMinutes(windowSizeInMinutes),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 30
                        })));

            // Rate limit rejection response with enhanced security logging
            options.OnRejected = async (context, token) =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var correlationId = context.HttpContext.Items["CorrelationId"]?.ToString() ?? "unknown";
                var userId = context.HttpContext.User?.FindFirst("sub")?.Value ?? "anonymous";

                // Enhanced logging for payment service security
                logger.LogWarning("PAYMENT_SECURITY: Rate limit exceeded for User: {UserId}, IP: {IP}, Path: {Path}, CorrelationId: {CorrelationId}",
                    userId,
                    context.HttpContext.Connection.RemoteIpAddress,
                    context.HttpContext.Request.Path,
                    correlationId);

                context.HttpContext.Response.StatusCode = 429;
                context.HttpContext.Response.Headers.RetryAfter = "60";

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Rate limit exceeded",
                    message = "Too many payment requests. Please try again later.",
                    retryAfter = 60,
                    correlationId
                }, cancellationToken: token);
            };
        });

        return services;
    }

    /// <summary>
    /// Get partition key for rate limiting with enhanced user context
    /// </summary>
    private static string GetPartitionKey(HttpContext httpContext)
    {
        // Skip rate limiting for health checks
        var path = httpContext.Request.Path.Value?.ToLowerInvariant();
        if (path != null && (path.StartsWith("/health") || path.StartsWith("/metrics")))
        {
            return "health-check";
        }

        // For payment service, we want more granular rate limiting
        // Use user ID if authenticated, otherwise use IP
        var userId = httpContext.User?.FindFirst("sub")?.Value ?? 
                    httpContext.User?.FindFirst("userId")?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        // For anonymous users, use IP address
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ipAddress}";
    }
}

/// <summary>
/// Attribute to apply specific rate limiting policies to payment controllers/actions
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PaymentRateLimitAttribute : Attribute
{
    public string Policy { get; }

    public PaymentRateLimitAttribute(string policy)
    {
        Policy = policy;
    }
}

/// <summary>
/// Extension methods for applying rate limiting to the payment service
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Apply rate limiting middleware to the payment service
    /// </summary>
    public static IApplicationBuilder UsePaymentServiceRateLimiting(this IApplicationBuilder app, IConfiguration configuration)
    {
        var rateLimitConfig = configuration.GetSection("RateLimiting");
        var isEnabled = rateLimitConfig.GetValue<bool>("Enabled");

        if (isEnabled)
        {
            app.UseRateLimiter();
        }

        return app;
    }
}
