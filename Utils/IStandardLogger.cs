using System.Diagnostics;

namespace PaymentService.Utils;

/// <summary>
/// Interface for standardized logger to enable testing and mocking
/// </summary>
public interface IStandardLogger
{
    /// <summary>
    /// Log an informational message
    /// </summary>
    void Info(string message, string? correlationId = null, object? metadata = null);

    /// <summary>
    /// Log a debug message
    /// </summary>
    void Debug(string message, string? correlationId = null, object? metadata = null);

    /// <summary>
    /// Log a warning message
    /// </summary>
    void Warn(string message, string? correlationId = null, object? metadata = null);

    /// <summary>
    /// Log an error message
    /// </summary>
    void Error(string message, Exception? exception = null, string? correlationId = null, object? metadata = null);

    /// <summary>
    /// Log a fatal error message
    /// </summary>
    void Fatal(string message, Exception? exception = null, string? correlationId = null, object? metadata = null);

    /// <summary>
    /// Start logging an operation and return a stopwatch for timing
    /// </summary>
    Stopwatch OperationStart(string operation, string? correlationId = null, object? metadata = null);

    /// <summary>
    /// Log operation completion
    /// </summary>
    void OperationComplete(string operation, Stopwatch stopwatch, string? correlationId = null, object? metadata = null);

    /// <summary>
    /// Log operation failure
    /// </summary>
    void OperationFailed(string operation, Stopwatch stopwatch, Exception exception, string? correlationId = null, object? metadata = null);

    /// <summary>
    /// Log business events
    /// </summary>
    void Business(string eventName, string? correlationId = null, object? metadata = null);

    /// <summary>
    /// Log security events
    /// </summary>
    void Security(string eventName, string? correlationId = null, object? metadata = null);

    /// <summary>
    /// Log performance metrics
    /// </summary>
    void Performance(string operation, long durationMs, string? correlationId = null, object? metadata = null);
}