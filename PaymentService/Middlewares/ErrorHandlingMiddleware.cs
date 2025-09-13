using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace PaymentService.Middlewares
{
    /// <summary>
    /// Global error handling middleware for centralized error handling and response formatting
    /// Provides environment-specific error details with proper production security
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IConfiguration _configuration;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var correlationId = context.Request.Headers["x-correlation-id"].FirstOrDefault();
                var environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";
                var isDevelopment = environment.Equals("Development", StringComparison.OrdinalIgnoreCase) ||
                                  environment.Equals("Local", StringComparison.OrdinalIgnoreCase);

                if (isDevelopment)
                {
                    _logger.LogError(ex, "An unhandled exception occurred: {Message} | CorrelationId: {CorrelationId}", 
                        ex.Message, correlationId);
                }
                else
                {
                    _logger.LogError("An unhandled exception occurred | CorrelationId: {CorrelationId} | Environment: {Environment}", 
                        correlationId, environment);
                }

                await HandleExceptionAsync(context, ex, correlationId ?? string.Empty, isDevelopment);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId, bool isDevelopment)
        {
            context.Response.ContentType = "application/json";

            object response;
            HttpStatusCode statusCode;

            switch (exception)
            {
                case ArgumentNullException:
                    statusCode = HttpStatusCode.BadRequest;
                    response = CreateErrorResponse("Invalid Request", 
                        isDevelopment ? exception.Message : "Invalid request parameters", correlationId);
                    break;
                case ArgumentException:
                    statusCode = HttpStatusCode.BadRequest;
                    response = CreateErrorResponse("Invalid Request", 
                        isDevelopment ? exception.Message : "Invalid request", correlationId);
                    break;
                case InvalidOperationException:
                    statusCode = HttpStatusCode.BadRequest;
                    response = CreateErrorResponse("Invalid Operation", 
                        isDevelopment ? exception.Message : "Invalid operation", correlationId);
                    break;
                case KeyNotFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    response = CreateErrorResponse("Resource Not Found", 
                        isDevelopment ? exception.Message : "The requested resource was not found", correlationId);
                    break;
                case UnauthorizedAccessException:
                    statusCode = HttpStatusCode.Unauthorized;
                    response = CreateErrorResponse("Unauthorized", 
                        isDevelopment ? exception.Message : "Unauthorized access", correlationId);
                    break;
                case TimeoutException:
                    statusCode = HttpStatusCode.RequestTimeout;
                    response = CreateErrorResponse("Request Timeout", 
                        isDevelopment ? exception.Message : "Request timeout", correlationId);
                    break;
                default:
                    statusCode = HttpStatusCode.InternalServerError;
                    response = CreateErrorResponse("Internal Server Error", 
                        isDevelopment ? exception.Message : "An unexpected error occurred", correlationId);
                    break;
            }

            context.Response.StatusCode = (int)statusCode;

            var jsonResponse = JsonSerializer.Serialize(response, _jsonOptions);
            await context.Response.WriteAsync(jsonResponse);
        }

        private static object CreateErrorResponse(string title, string detail, string correlationId)
        {
            var response = new
            {
                title,
                detail,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                status = (int)HttpStatusCode.InternalServerError
            };

            if (!string.IsNullOrEmpty(correlationId))
            {
                return new
                {
                    title = response.title,
                    detail = response.detail,
                    timestamp = response.timestamp,
                    status = response.status,
                    correlationId = correlationId
                };
            }

            return response;
        }
    }
}