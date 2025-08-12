using Microsoft.Extensions.Logging;
using Serilog;
using System.Diagnostics;
using System.Text.Json;

namespace PaymentService.Utils
{
    /// <summary>
    /// Composite disposable to handle multiple IDisposable objects
    /// </summary>
    public class CompositeDisposable : IDisposable
    {
        private readonly List<IDisposable> _disposables;
        private bool _disposed = false;

        public CompositeDisposable(IEnumerable<IDisposable> disposables)
        {
            _disposables = disposables.ToList();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var disposable in _disposables)
                {
                    disposable?.Dispose();
                }
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Standardized logger for Payment Service with correlation ID and structured logging support
    /// </summary>
    public class StandardLogger
    {
        private readonly ILogger<StandardLogger> _logger;
        private readonly string _serviceName;

        public StandardLogger(ILogger<StandardLogger> logger, IConfiguration configuration)
        {
            _logger = logger;
            _serviceName = configuration["ServiceName"] ?? "payment-service";
        }

        /// <summary>
        /// Log an informational message
        /// </summary>
        public void Info(string message, string? correlationId = null, object? metadata = null)
        {
            using (CreateLogContext(correlationId, metadata))
            {
                _logger.LogInformation(message);
            }
        }

        /// <summary>
        /// Log a debug message
        /// </summary>
        public void Debug(string message, string? correlationId = null, object? metadata = null)
        {
            using (CreateLogContext(correlationId, metadata))
            {
                _logger.LogDebug(message);
            }
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public void Warn(string message, string? correlationId = null, object? metadata = null)
        {
            using (CreateLogContext(correlationId, metadata))
            {
                _logger.LogWarning(message);
            }
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public void Error(string message, Exception? exception = null, string? correlationId = null, object? metadata = null)
        {
            using (CreateLogContext(correlationId, metadata))
            {
                if (exception != null)
                {
                    _logger.LogError(exception, message);
                }
                else
                {
                    _logger.LogError(message);
                }
            }
        }

        /// <summary>
        /// Log a fatal error message
        /// </summary>
        public void Fatal(string message, Exception? exception = null, string? correlationId = null, object? metadata = null)
        {
            using (CreateLogContext(correlationId, metadata))
            {
                if (exception != null)
                {
                    _logger.LogCritical(exception, message);
                }
                else
                {
                    _logger.LogCritical(message);
                }
            }
        }

        /// <summary>
        /// Start logging an operation and return a stopwatch for timing
        /// </summary>
        public Stopwatch OperationStart(string operation, string? correlationId = null, object? metadata = null)
        {
            var stopwatch = Stopwatch.StartNew();
            using (CreateLogContext(correlationId, CombineMetadata(metadata, new { operation, event_type = "operation_start" })))
            {
                _logger.LogInformation("Operation started: {Operation}", operation);
            }
            return stopwatch;
        }

        /// <summary>
        /// Log operation completion
        /// </summary>
        public void OperationComplete(string operation, Stopwatch stopwatch, string? correlationId = null, object? metadata = null)
        {
            stopwatch.Stop();
            using (CreateLogContext(correlationId, CombineMetadata(metadata, new { 
                operation, 
                duration_ms = stopwatch.ElapsedMilliseconds,
                event_type = "operation_complete"
            })))
            {
                _logger.LogInformation("Operation completed: {Operation} in {Duration}ms", operation, stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Log operation failure
        /// </summary>
        public void OperationFailed(string operation, Stopwatch stopwatch, Exception exception, string? correlationId = null, object? metadata = null)
        {
            stopwatch.Stop();
            using (CreateLogContext(correlationId, CombineMetadata(metadata, new { 
                operation, 
                duration_ms = stopwatch.ElapsedMilliseconds,
                event_type = "operation_failed",
                error = exception.Message
            })))
            {
                _logger.LogError(exception, "Operation failed: {Operation} after {Duration}ms", operation, stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Log business events
        /// </summary>
        public void Business(string eventName, string? correlationId = null, object? metadata = null)
        {
            using (CreateLogContext(correlationId, CombineMetadata(metadata, new { 
                event_name = eventName,
                event_type = "business",
                event_category = "business_event"
            })))
            {
                _logger.LogInformation("Business event: {EventName}", eventName);
            }
        }

        /// <summary>
        /// Log security events
        /// </summary>
        public void Security(string eventName, string? correlationId = null, object? metadata = null)
        {
            using (CreateLogContext(correlationId, CombineMetadata(metadata, new { 
                event_name = eventName,
                event_type = "security",
                event_category = "security_event"
            })))
            {
                _logger.LogWarning("Security event: {EventName}", eventName);
            }
        }

        /// <summary>
        /// Log performance metrics
        /// </summary>
        public void Performance(string operation, long durationMs, string? correlationId = null, object? metadata = null)
        {
            using (CreateLogContext(correlationId, CombineMetadata(metadata, new { 
                operation,
                duration_ms = durationMs,
                event_type = "performance",
                event_category = "performance_metric"
            })))
            {
                _logger.LogInformation("Performance metric: {Operation} took {Duration}ms", operation, durationMs);
            }
        }

        /// <summary>
        /// Create a logging context with correlation ID and metadata
        /// </summary>
        private IDisposable CreateLogContext(string? correlationId, object? metadata)
        {
            var properties = new List<(string, object)>
            {
                ("Service", _serviceName)
            };

            if (!string.IsNullOrEmpty(correlationId))
            {
                properties.Add(("CorrelationId", correlationId));
            }

            if (metadata != null)
            {
                // Convert metadata to key-value pairs
                var metadataJson = JsonSerializer.Serialize(metadata);
                var metadataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson);
                
                if (metadataDict != null)
                {
                    foreach (var kvp in metadataDict)
                    {
                        properties.Add((kvp.Key, kvp.Value));
                    }
                }
            }

            // Create property enrichers for Serilog context
            var enrichers = new List<IDisposable>();
            
            foreach (var property in properties)
            {
                enrichers.Add(Serilog.Context.LogContext.PushProperty(property.Item1, property.Item2));
            }
            
            return new CompositeDisposable(enrichers);
        }

        /// <summary>
        /// Combine metadata objects
        /// </summary>
        private object CombineMetadata(object? existingMetadata, object newMetadata)
        {
            if (existingMetadata == null)
                return newMetadata;

            var existingDict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(existingMetadata)) ?? new Dictionary<string, object>();
            var newDict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(newMetadata)) ?? new Dictionary<string, object>();

            foreach (var kvp in newDict)
            {
                existingDict[kvp.Key] = kvp.Value;
            }

            return existingDict;
        }
    }
}
