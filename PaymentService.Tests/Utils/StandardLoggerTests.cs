using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentService.Utils;
using System.Diagnostics;
using Xunit;

namespace PaymentService.Tests.Utils;

/// <summary>
/// Unit tests for StandardLogger to verify enhanced logging functionality
/// </summary>
public class StandardLoggerTests
{
    private readonly Mock<ILogger<StandardLogger>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly StandardLogger _standardLogger;

    public StandardLoggerTests()
    {
        _mockLogger = new Mock<ILogger<StandardLogger>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(x => x["ServiceName"]).Returns("payment-service-test");
        
        _standardLogger = new StandardLogger(_mockLogger.Object, _mockConfiguration.Object);
    }

    [Fact]
    public void Info_WithCorrelationIdAndMetadata_LogsInformation()
    {
        // Arrange
        var message = "Test information message";
        var correlationId = "test-correlation-id";
        var metadata = new { orderId = "12345", amount = 100.50m };

        // Act
        _standardLogger.Info(message, correlationId, metadata);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Debug_WithCorrelationId_LogsDebugMessage()
    {
        // Arrange
        var message = "Debug message";
        var correlationId = "debug-correlation-id";

        // Act
        _standardLogger.Debug(message, correlationId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Warn_WithCorrelationIdAndMetadata_LogsWarning()
    {
        // Arrange
        var message = "Warning message";
        var correlationId = "warn-correlation-id";
        var metadata = new { validationError = "missing_order_id" };

        // Act
        _standardLogger.Warn(message, correlationId, metadata);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Error_WithExceptionAndCorrelationId_LogsError()
    {
        // Arrange
        var message = "Error occurred";
        var exception = new InvalidOperationException("Test exception");
        var correlationId = "error-correlation-id";

        // Act
        _standardLogger.Error(message, exception, correlationId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Fatal_WithExceptionAndCorrelationId_LogsCritical()
    {
        // Arrange
        var message = "Fatal error occurred";
        var exception = new Exception("Critical exception");
        var correlationId = "fatal-correlation-id";

        // Act
        _standardLogger.Fatal(message, exception, correlationId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void OperationStart_ReturnsStopwatchAndLogsStart()
    {
        // Arrange
        var operation = "TEST_OPERATION";
        var correlationId = "operation-correlation-id";
        var metadata = new { operation = "TEST_OPERATION" };

        // Act
        var stopwatch = _standardLogger.OperationStart(operation, correlationId, metadata);

        // Assert
        Assert.NotNull(stopwatch);
        Assert.True(stopwatch.IsRunning);
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void OperationComplete_StopsStopwatchAndLogsCompletion()
    {
        // Arrange
        var operation = "COMPLETE_OPERATION";
        var correlationId = "complete-correlation-id";
        var stopwatch = Stopwatch.StartNew();
        Thread.Sleep(10); // Ensure some time has passed
        var metadata = new { paymentId = Guid.NewGuid() };

        // Act
        _standardLogger.OperationComplete(operation, stopwatch, correlationId, metadata);

        // Assert
        Assert.False(stopwatch.IsRunning);
        Assert.True(stopwatch.ElapsedMilliseconds > 0);
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void OperationFailed_StopsStopwatchAndLogsFailure()
    {
        // Arrange
        var operation = "FAILED_OPERATION";
        var correlationId = "failed-correlation-id";
        var stopwatch = Stopwatch.StartNew();
        Thread.Sleep(10);
        var exception = new Exception("Operation failed");
        var metadata = new { paymentId = Guid.NewGuid() };

        // Act
        _standardLogger.OperationFailed(operation, stopwatch, exception, correlationId, metadata);

        // Assert
        Assert.False(stopwatch.IsRunning);
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Business_LogsBusinessEvent()
    {
        // Arrange
        var eventName = "PAYMENT_PROCESSED";
        var correlationId = "business-correlation-id";
        var metadata = new { 
            paymentId = Guid.NewGuid(),
            amount = 150.75m,
            currency = "USD"
        };

        // Act
        _standardLogger.Business(eventName, correlationId, metadata);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Security_LogsSecurityEvent()
    {
        // Arrange
        var eventName = "UNAUTHORIZED_ACCESS_ATTEMPT";
        var correlationId = "security-correlation-id";
        var metadata = new { 
            userId = "user123",
            ipAddress = "192.168.1.1"
        };

        // Act
        _standardLogger.Security(eventName, correlationId, metadata);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Performance_LogsPerformanceMetric()
    {
        // Arrange
        var operation = "DATABASE_QUERY";
        var durationMs = 250L;
        var correlationId = "performance-correlation-id";
        var metadata = new { 
            queryType = "SELECT",
            tableCount = 3
        };

        // Act
        _standardLogger.Performance(operation, durationMs, correlationId, metadata);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Info_WithoutCorrelationId_LogsSuccessfully()
    {
        // Arrange
        var message = "Test message without correlation ID";

        // Act
        _standardLogger.Info(message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Error_WithoutException_LogsErrorMessage()
    {
        // Arrange
        var message = "Error without exception";
        var correlationId = "error-no-exception";

        // Act
        _standardLogger.Error(message, null, correlationId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}