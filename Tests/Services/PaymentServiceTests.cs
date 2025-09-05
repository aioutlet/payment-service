using Microsoft.EntityFrameworkCore;
using Moq;
using PaymentService.Data;
using PaymentService.Models.DTOs;
using PaymentService.Models.Entities;
using PaymentService.Services.Providers;
using PaymentService.Utils;
using Xunit;

namespace PaymentService.Tests.Services;

/// <summary>
/// Unit tests for PaymentService with focus on enhanced logging functionality
/// </summary>
public class PaymentServiceTests : IDisposable
{
    private readonly PaymentDbContext _dbContext;
    private readonly Mock<IPaymentProviderFactory> _mockProviderFactory;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IStandardLogger> _mockLogger;
    private readonly Mock<IPaymentProvider> _mockPaymentProvider;
    private readonly PaymentService.Services.PaymentService _paymentService;

    public PaymentServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new PaymentDbContext(options);

        // Setup mocks
        _mockProviderFactory = new Mock<IPaymentProviderFactory>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockLogger = new Mock<IStandardLogger>();
        _mockPaymentProvider = new Mock<IPaymentProvider>();

        // Setup mock provider
        _mockPaymentProvider.Setup(x => x.ProviderName).Returns("test-provider");
        _mockProviderFactory.Setup(x => x.GetDefaultProvider()).Returns(_mockPaymentProvider.Object);
        _mockProviderFactory.Setup(x => x.GetProvider(It.IsAny<string>())).Returns(_mockPaymentProvider.Object);

        // Setup current user
        _mockCurrentUserService.Setup(x => x.UserId).Returns("test-user-123");

        _paymentService = new PaymentService.Services.PaymentService(
            _dbContext,
            _mockProviderFactory.Object,
            _mockCurrentUserService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ValidRequest_LogsOperationStartAndComplete()
    {
        // Arrange
        var request = new ProcessPaymentDto
        {
            OrderId = "order-123",
            CustomerId = "customer-456",
            Amount = 100.50m,
            Currency = "USD",
            PaymentMethod = "visa"
        };

        var providerResult = new PaymentProviderResult
        {
            IsSuccess = true,
            Status = PaymentStatus.Succeeded,
            TransactionId = "txn-123",
            ProviderTransactionId = "provider-txn-456"
        };

        _mockPaymentProvider.Setup(x => x.ProcessPaymentAsync(It.IsAny<ProcessPaymentDto>(), It.IsAny<string>()))
            .ReturnsAsync(providerResult);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        
        // Verify operation start was logged
        _mockLogger.Verify(x => x.OperationStart(
            "PROCESS_PAYMENT",
            It.IsAny<string>(),
            It.Is<object>(o => o.ToString()!.Contains("PROCESS_PAYMENT"))), 
            Times.Once);

        // Verify operation complete was logged
        _mockLogger.Verify(x => x.OperationComplete(
            "PROCESS_PAYMENT",
            It.IsAny<System.Diagnostics.Stopwatch>(),
            It.IsAny<string>(),
            It.IsAny<object>()), 
            Times.Once);

        // Verify business event was logged
        _mockLogger.Verify(x => x.Business(
            "PAYMENT_PROCESSED",
            It.IsAny<string>(),
            It.IsAny<object>()), 
            Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_InvalidOrderId_LogsValidationWarning()
    {
        // Arrange
        var request = new ProcessPaymentDto
        {
            OrderId = "", // Invalid empty order ID
            CustomerId = "customer-456",
            Amount = 100.50m,
            Currency = "USD"
        };

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Order ID is required", result.ErrorMessage);

        // Verify operation start was logged
        _mockLogger.Verify(x => x.OperationStart(
            "PROCESS_PAYMENT",
            It.IsAny<string>(),
            It.IsAny<object>()), 
            Times.Once);

        // Verify validation warning was logged
        _mockLogger.Verify(x => x.Warn(
            "Payment validation failed: Order ID is required",
            It.IsAny<string>(),
            It.Is<object>(o => o.ToString()!.Contains("missing_order_id"))), 
            Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_InvalidAmount_LogsValidationWarning()
    {
        // Arrange
        var request = new ProcessPaymentDto
        {
            OrderId = "order-123",
            CustomerId = "customer-456",
            Amount = -50.00m, // Invalid negative amount
            Currency = "USD"
        };

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Payment amount must be greater than zero", result.ErrorMessage);

        // Verify validation warning was logged with amount details
        _mockLogger.Verify(x => x.Warn(
            "Payment validation failed: Payment amount must be greater than zero",
            It.IsAny<string>(),
            It.Is<object>(o => o.ToString()!.Contains("invalid_amount"))), 
            Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_DuplicatePayment_LogsDuplicateWarning()
    {
        // Arrange
        var orderId = "duplicate-order-123";
        
        // Create existing payment in database
        var existingPayment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Status = PaymentStatus.Succeeded,
            Amount = 100.00m,
            Currency = "USD",
            PaymentMethod = "visa",
            Provider = "test-provider",
            CustomerId = "customer-456",
            CreatedBy = "user-123",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Payments.Add(existingPayment);
        await _dbContext.SaveChangesAsync();

        var request = new ProcessPaymentDto
        {
            OrderId = orderId,
            CustomerId = "customer-456",
            Amount = 100.50m,
            Currency = "USD"
        };

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Payment already exists for this order", result.ErrorMessage);

        // Verify duplicate warning was logged
        _mockLogger.Verify(x => x.Warn(
            "Duplicate payment attempt detected",
            It.IsAny<string>(),
            It.Is<object>(o => o.ToString()!.Contains("payment_already_exists"))), 
            Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ProviderException_LogsOperationFailed()
    {
        // Arrange
        var request = new ProcessPaymentDto
        {
            OrderId = "order-exception-test",
            CustomerId = "customer-456",
            Amount = 100.50m,
            Currency = "USD"
        };

        var exception = new InvalidOperationException("Provider service unavailable");
        _mockPaymentProvider.Setup(x => x.ProcessPaymentAsync(It.IsAny<ProcessPaymentDto>(), It.IsAny<string>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("An unexpected error occurred while processing the payment", result.ErrorMessage);

        // Verify operation failed was logged
        _mockLogger.Verify(x => x.OperationFailed(
            "PROCESS_PAYMENT",
            It.IsAny<System.Diagnostics.Stopwatch>(),
            It.IsAny<Exception>(),
            It.IsAny<string>(),
            It.IsAny<object>()), 
            Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_SuccessfulPayment_LogsProviderSelection()
    {
        // Arrange
        var request = new ProcessPaymentDto
        {
            OrderId = "order-provider-test",
            CustomerId = "customer-456",
            Amount = 100.50m,
            Currency = "USD",
            PaymentMethod = "mastercard"
        };

        var providerResult = new PaymentProviderResult
        {
            IsSuccess = true,
            Status = PaymentStatus.Succeeded,
            TransactionId = "txn-789"
        };

        _mockPaymentProvider.Setup(x => x.ProcessPaymentAsync(It.IsAny<ProcessPaymentDto>(), It.IsAny<string>()))
            .ReturnsAsync(providerResult);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify provider selection was logged
        _mockLogger.Verify(x => x.Info(
            "Payment provider selected",
            It.IsAny<string>(),
            It.Is<object>(o => o.ToString()!.Contains("test-provider"))), 
            Times.Once);

        // Verify payment record creation was logged
        _mockLogger.Verify(x => x.Info(
            "Payment record created",
            It.IsAny<string>(),
            It.IsAny<object>()), 
            Times.Once);
    }

    [Fact]
    public async Task ProcessRefundAsync_ValidRequest_LogsOperationStartAndComplete()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = paymentId,
            OrderId = "order-refund-test",
            Status = PaymentStatus.Succeeded,
            Amount = 200.00m,
            Currency = "USD",
            PaymentMethod = "visa",
            Provider = "test-provider",
            CustomerId = "customer-456",
            CreatedBy = "user-123",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync();

        var request = new ProcessRefundDto
        {
            PaymentId = paymentId.ToString(),
            Amount = 100.00m,
            Reason = "Customer request"
        };

        var refundResult = new RefundProviderResult
        {
            IsSuccess = true,
            Status = RefundStatus.Succeeded,
            ProviderRefundId = "refund-123"
        };

        _mockPaymentProvider.Setup(x => x.ProcessRefundAsync(
            It.IsAny<Payment>(),
            It.IsAny<decimal>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(refundResult);

        // Act
        var result = await _paymentService.ProcessRefundAsync(request);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify operation start was logged
        _mockLogger.Verify(x => x.OperationStart(
            "PROCESS_REFUND",
            It.IsAny<string>(),
            It.Is<object>(o => o.ToString()!.Contains("PROCESS_REFUND"))), 
            Times.Once);

        // Verify operation complete was logged
        _mockLogger.Verify(x => x.OperationComplete(
            "PROCESS_REFUND",
            It.IsAny<System.Diagnostics.Stopwatch>(),
            It.IsAny<string>(),
            It.IsAny<object>()), 
            Times.Once);

        // Verify business event was logged
        _mockLogger.Verify(x => x.Business(
            "REFUND_PROCESSED",
            It.IsAny<string>(),
            It.IsAny<object>()), 
            Times.Once);
    }

    [Fact]
    public async Task ProcessRefundAsync_PaymentNotFound_LogsValidationWarning()
    {
        // Arrange
        var request = new ProcessRefundDto
        {
            PaymentId = Guid.NewGuid().ToString(),
            Amount = 50.00m,
            Reason = "Customer request"
        };

        // Act
        var result = await _paymentService.ProcessRefundAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Payment not found", result.ErrorMessage);

        // Verify validation warning was logged
        _mockLogger.Verify(x => x.Warn(
            "Refund validation failed: Payment not found",
            It.IsAny<string>(),
            It.Is<object>(o => o.ToString()!.Contains("payment_not_found"))), 
            Times.Once);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}