using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Models.DTOs;
using PaymentService.Models.Entities;
using PaymentService.Services.Providers;
using PaymentService.Utils;

namespace PaymentService.Services;

/// <summary>
/// Main payment service for processing payments, refunds, and managing payment methods
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly PaymentDbContext _dbContext;
    private readonly IPaymentProviderFactory _providerFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        PaymentDbContext dbContext,
        IPaymentProviderFactory providerFactory,
        ICurrentUserService currentUserService,
        ILogger<PaymentService> logger)
    {
        _dbContext = dbContext;
        _providerFactory = providerFactory;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentDto request)
    {
        var correlationId = CorrelationIdHelper.GetCorrelationId();
        var currentUserId = _currentUserService.UserId;

        _logger.LogInformation("Processing payment for order {OrderId} by user {UserId} [CorrelationId: {CorrelationId}]", 
            request.OrderId, currentUserId, correlationId);

        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.OrderId))
            {
                return new PaymentResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = "Order ID is required"
                };
            }

            if (request.Amount <= 0)
            {
                return new PaymentResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = "Payment amount must be greater than zero"
                };
            }

            // Check for duplicate payments
            var existingPayment = await _dbContext.Payments
                .FirstOrDefaultAsync(p => p.OrderId == request.OrderId && 
                                        p.Status == PaymentStatus.Succeeded);

            if (existingPayment != null)
            {
                _logger.LogWarning("Duplicate payment attempt for order {OrderId} [CorrelationId: {CorrelationId}]", 
                    request.OrderId, correlationId);
                
                return new PaymentResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = "Payment already exists for this order"
                };
            }

            // Get payment provider
            IPaymentProvider provider;
            if (!string.IsNullOrWhiteSpace(request.PaymentProvider))
            {
                provider = _providerFactory.GetProvider(request.PaymentProvider);
            }
            else if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
            {
                provider = _providerFactory.GetProviderForPaymentMethod(request.PaymentMethod);
            }
            else
            {
                provider = _providerFactory.GetDefaultProvider();
            }

            // Create payment record
            var payment = new Payment
            {
                OrderId = request.OrderId,
                CustomerId = request.CustomerId,
                Amount = request.Amount,
                Currency = request.Currency.ToUpper(),
                PaymentMethod = request.PaymentMethod ?? "unknown",
                Provider = provider.ProviderName,
                Status = PaymentStatus.Pending,
                Description = request.Description,
                CreatedBy = currentUserId,
                Metadata = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["correlation_id"] = correlationId,
                    ["user_id"] = currentUserId
                })
            };

            _dbContext.Payments.Add(payment);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Payment record created with ID {PaymentId} [CorrelationId: {CorrelationId}]", 
                payment.Id, correlationId);

            // Process payment with provider
            var providerResult = await provider.ProcessPaymentAsync(request, correlationId);

            // Update payment record with provider response
            payment.ProviderTransactionId = providerResult.ProviderTransactionId;
            payment.Status = providerResult.Status;
            payment.FailureReason = providerResult.FailureReason;
            
            // Merge metadata
            if (providerResult.Metadata != null)
            {
                var existingMetadata = string.IsNullOrEmpty(payment.Metadata) 
                    ? new Dictionary<string, object>() 
                    : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(payment.Metadata) ?? new Dictionary<string, object>();
                    
                foreach (var kvp in providerResult.Metadata)
                {
                    existingMetadata[kvp.Key] = kvp.Value;
                }
                
                payment.Metadata = System.Text.Json.JsonSerializer.Serialize(existingMetadata);
            }

            payment.UpdatedAt = DateTime.UtcNow;
            payment.UpdatedBy = currentUserId;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Payment {PaymentId} updated with provider result. Success: {IsSuccess}, Status: {Status} [CorrelationId: {CorrelationId}]", 
                payment.Id, providerResult.IsSuccess, providerResult.Status, correlationId);

            return new PaymentResultDto
            {
                PaymentId = payment.Id.ToString(),
                TransactionId = providerResult.TransactionId,
                ProviderTransactionId = providerResult.ProviderTransactionId,
                Status = providerResult.Status,
                IsSuccess = providerResult.IsSuccess,
                ErrorMessage = providerResult.FailureReason,
                PaymentProvider = provider.ProviderName,
                Amount = payment.Amount,
                Currency = payment.Currency,
                ProcessedAt = payment.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for order {OrderId} [CorrelationId: {CorrelationId}]", 
                request.OrderId, correlationId);

            return new PaymentResultDto
            {
                IsSuccess = false,
                ErrorMessage = "An unexpected error occurred while processing the payment"
            };
        }
    }

    public async Task<RefundResultDto> ProcessRefundAsync(ProcessRefundDto request)
    {
        var correlationId = CorrelationIdHelper.GetCorrelationId();
        var currentUserId = _currentUserService.UserId;

        _logger.LogInformation("Processing refund for payment {PaymentId} by user {UserId} [CorrelationId: {CorrelationId}]", 
            request.PaymentId, currentUserId, correlationId);

        try
        {
            // Get original payment
            var payment = await _dbContext.Payments
                .FirstOrDefaultAsync(p => p.Id.ToString() == request.PaymentId);

            if (payment == null)
            {
                return new RefundResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = "Payment not found"
                };
            }

            if (payment.Status != PaymentStatus.Succeeded)
            {
                return new RefundResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = "Only successful payments can be refunded"
                };
            }

            // Calculate refund amount
            var refundAmount = request.Amount;
            
            // Check refund amount is valid
            var totalRefunded = await _dbContext.PaymentRefunds
                .Where(r => r.PaymentId == payment.Id && r.Status == RefundStatus.Succeeded)
                .SumAsync(r => r.Amount);

            if (totalRefunded + refundAmount > payment.Amount)
            {
                return new RefundResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = "Refund amount exceeds available balance"
                };
            }

            // Get payment provider
            var provider = _providerFactory.GetProvider(payment.Provider);

            // Create refund record
            var refund = new PaymentRefund
            {
                PaymentId = payment.Id,
                Amount = refundAmount,
                Currency = payment.Currency,
                Reason = request.Reason,
                Status = RefundStatus.Processing,
                CreatedBy = currentUserId,
                Metadata = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["correlation_id"] = correlationId,
                    ["user_id"] = currentUserId
                })
            };

            _dbContext.PaymentRefunds.Add(refund);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Refund record created with ID {RefundId} [CorrelationId: {CorrelationId}]", 
                refund.Id, correlationId);

            // Process refund with provider
            var providerResult = await provider.ProcessRefundAsync(payment, refundAmount, request.Reason ?? "Refund requested", correlationId);

            // Update refund record with provider response
            refund.ProviderRefundId = providerResult.ProviderRefundId;
            refund.Status = providerResult.Status;
            refund.FailureReason = providerResult.FailureReason;
            
            // Merge metadata
            if (providerResult.Metadata != null)
            {
                var existingMetadata = string.IsNullOrEmpty(refund.Metadata) 
                    ? new Dictionary<string, object>() 
                    : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(refund.Metadata) ?? new Dictionary<string, object>();
                    
                foreach (var kvp in providerResult.Metadata)
                {
                    existingMetadata[kvp.Key] = kvp.Value;
                }
                
                refund.Metadata = System.Text.Json.JsonSerializer.Serialize(existingMetadata);
            }

            refund.UpdatedAt = DateTime.UtcNow;
            refund.UpdatedBy = currentUserId;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Refund {RefundId} updated with provider result. Success: {IsSuccess}, Status: {Status} [CorrelationId: {CorrelationId}]", 
                refund.Id, providerResult.IsSuccess, providerResult.Status, correlationId);

            return new RefundResultDto
            {
                RefundId = refund.Id.ToString(),
                PaymentId = payment.Id.ToString(),
                ProviderRefundId = providerResult.ProviderRefundId,
                Status = providerResult.Status,
                IsSuccess = providerResult.IsSuccess,
                ErrorMessage = providerResult.FailureReason,
                Amount = refund.Amount,
                Currency = refund.Currency,
                ProcessedAt = refund.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId} [CorrelationId: {CorrelationId}]", 
                request.PaymentId, correlationId);

            return new RefundResultDto
            {
                IsSuccess = false,
                ErrorMessage = "An unexpected error occurred while processing the refund"
            };
        }
    }

    public async Task<SavePaymentMethodResultDto> SavePaymentMethodAsync(SavePaymentMethodDto request)
    {
        var correlationId = CorrelationIdHelper.GetCorrelationId();
        var currentUserId = _currentUserService.UserId;

        _logger.LogInformation("Saving payment method for customer {CustomerId} by user {UserId} [CorrelationId: {CorrelationId}]", 
            request.CustomerId, currentUserId, correlationId);

        try
        {
            // Get payment provider
            var provider = !string.IsNullOrWhiteSpace(request.PaymentProvider)
                ? _providerFactory.GetProvider(request.PaymentProvider)
                : _providerFactory.GetDefaultProvider();

            // Save payment method with provider
            var providerResult = await provider.SavePaymentMethodAsync(request, correlationId);

            if (!providerResult.IsSuccess)
            {
                return new SavePaymentMethodResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = providerResult.FailureReason
                };
            }

            // Create payment method record
            var paymentMethod = new PaymentMethod
            {
                CustomerId = request.CustomerId,
                Provider = provider.ProviderName,
                ProviderTokenId = providerResult.ProviderTokenId ?? string.Empty,
                Type = request.PaymentMethodType,
                Last4Digits = providerResult.Last4Digits,
                Brand = providerResult.Brand ?? string.Empty,
                ExpiryMonth = providerResult.ExpiryMonth,
                ExpiryYear = providerResult.ExpiryYear,
                IsDefault = request.IsDefault,
                CreatedBy = currentUserId,
                Metadata = System.Text.Json.JsonSerializer.Serialize(providerResult.Metadata ?? new Dictionary<string, object>())
            };

            // If this is set as default, unset other default methods for this customer
            if (request.IsDefault)
            {
                var existingDefaults = await _dbContext.PaymentMethods
                    .Where(pm => pm.CustomerId == request.CustomerId && pm.IsDefault)
                    .ToListAsync();

                foreach (var existingDefault in existingDefaults)
                {
                    existingDefault.IsDefault = false;
                    existingDefault.UpdatedAt = DateTime.UtcNow;
                    existingDefault.UpdatedBy = currentUserId;
                }
            }

            _dbContext.PaymentMethods.Add(paymentMethod);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Payment method saved with ID {PaymentMethodId} [CorrelationId: {CorrelationId}]", 
                paymentMethod.Id, correlationId);

            return new SavePaymentMethodResultDto
            {
                PaymentMethodId = paymentMethod.Id.ToString(),
                ProviderTokenId = paymentMethod.ProviderTokenId,
                Last4Digits = paymentMethod.Last4Digits,
                Brand = paymentMethod.Brand,
                ExpiryMonth = paymentMethod.ExpiryMonth,
                ExpiryYear = paymentMethod.ExpiryYear,
                IsDefault = paymentMethod.IsDefault,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving payment method for customer {CustomerId} [CorrelationId: {CorrelationId}]", 
                request.CustomerId, correlationId);

            return new SavePaymentMethodResultDto
            {
                IsSuccess = false,
                ErrorMessage = "An unexpected error occurred while saving the payment method"
            };
        }
    }

    public async Task<bool> DeletePaymentMethodAsync(Guid paymentMethodId)
    {
        var correlationId = CorrelationIdHelper.GetCorrelationId();
        var currentUserId = _currentUserService.UserId;

        _logger.LogInformation("Deleting payment method {PaymentMethodId} by user {UserId} [CorrelationId: {CorrelationId}]", 
            paymentMethodId, currentUserId, correlationId);

        try
        {
            var paymentMethod = await _dbContext.PaymentMethods
                .FirstOrDefaultAsync(pm => pm.Id == paymentMethodId);

            if (paymentMethod == null)
            {
                _logger.LogWarning("Payment method {PaymentMethodId} not found [CorrelationId: {CorrelationId}]", 
                    paymentMethodId, correlationId);
                return false;
            }

            // Delete from provider
            var provider = _providerFactory.GetProvider(paymentMethod.Provider);
            var providerDeleted = await provider.DeletePaymentMethodAsync(paymentMethod.ProviderTokenId, correlationId);

            if (!providerDeleted)
            {
                _logger.LogWarning("Failed to delete payment method from provider {Provider} [CorrelationId: {CorrelationId}]", 
                    paymentMethod.Provider, correlationId);
            }

            // Delete from database regardless of provider response
            _dbContext.PaymentMethods.Remove(paymentMethod);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Payment method {PaymentMethodId} deleted [CorrelationId: {CorrelationId}]", 
                paymentMethodId, correlationId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment method {PaymentMethodId} [CorrelationId: {CorrelationId}]", 
                paymentMethodId, correlationId);
            return false;
        }
    }

    public async Task<List<PaymentMethodDto>> GetPaymentMethodsAsync(string customerId)
    {
        var correlationId = CorrelationIdHelper.GetCorrelationId();

        _logger.LogInformation("Getting payment methods for customer {CustomerId} [CorrelationId: {CorrelationId}]", 
            customerId, correlationId);

        try
        {
            var paymentMethods = await _dbContext.PaymentMethods
                .Where(pm => pm.CustomerId == customerId)
                .OrderByDescending(pm => pm.IsDefault)
                .ThenByDescending(pm => pm.CreatedAt)
                .Select(pm => new PaymentMethodDto
                {
                    Id = pm.Id.ToString(),
                    PaymentProvider = pm.Provider,
                    PaymentMethodType = pm.Type,
                    Last4Digits = pm.Last4Digits,
                    Brand = pm.Brand,
                    ExpiryMonth = pm.ExpiryMonth,
                    ExpiryYear = pm.ExpiryYear,
                    IsDefault = pm.IsDefault,
                    CreatedAt = pm.CreatedAt
                })
                .ToListAsync();

            _logger.LogInformation("Found {Count} payment methods for customer {CustomerId} [CorrelationId: {CorrelationId}]", 
                paymentMethods.Count, customerId, correlationId);

            return paymentMethods;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment methods for customer {CustomerId} [CorrelationId: {CorrelationId}]", 
                customerId, correlationId);
            return new List<PaymentMethodDto>();
        }
    }

    public async Task<PaymentDto?> GetPaymentAsync(Guid paymentId)
    {
        var correlationId = CorrelationIdHelper.GetCorrelationId();

        _logger.LogInformation("Getting payment {PaymentId} [CorrelationId: {CorrelationId}]", 
            paymentId, correlationId);

        try
        {
            var payment = await _dbContext.Payments
                .Include(p => p.Refunds)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                _logger.LogWarning("Payment {PaymentId} not found [CorrelationId: {CorrelationId}]", 
                    paymentId, correlationId);
                return null;
            }

            return new PaymentDto
            {
                Id = payment.Id.ToString(),
                OrderId = payment.OrderId,
                CustomerId = payment.CustomerId,
                Amount = payment.Amount,
                Currency = payment.Currency,
                PaymentMethod = payment.PaymentMethod,
                PaymentProvider = payment.Provider,
                Status = payment.Status,
                ProviderTransactionId = payment.ProviderTransactionId,
                FailureReason = payment.FailureReason,
                Description = payment.Description,
                CreatedAt = payment.CreatedAt,
                Refunds = payment.Refunds.Select(r => new RefundDto
                {
                    Id = r.Id.ToString(),
                    Amount = r.Amount,
                    Currency = r.Currency,
                    Status = r.Status,
                    Reason = r.Reason,
                    ProviderRefundId = r.ProviderRefundId,
                    FailureReason = r.FailureReason,
                    CreatedAt = r.CreatedAt
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment {PaymentId} [CorrelationId: {CorrelationId}]", 
                paymentId, correlationId);
            return null;
        }
    }

    public async Task<List<PaymentDto>> GetPaymentsAsync(string? customerId = null, string? orderId = null, int skip = 0, int take = 50)
    {
        var correlationId = CorrelationIdHelper.GetCorrelationId();

        _logger.LogInformation("Getting payments - CustomerId: {CustomerId}, OrderId: {OrderId}, Skip: {Skip}, Take: {Take} [CorrelationId: {CorrelationId}]", 
            customerId, orderId, skip, take, correlationId);

        try
        {
            var query = _dbContext.Payments.Include(p => p.Refunds).AsQueryable();

            if (!string.IsNullOrWhiteSpace(customerId))
            {
                query = query.Where(p => p.CustomerId == customerId);
            }

            if (!string.IsNullOrWhiteSpace(orderId))
            {
                query = query.Where(p => p.OrderId == orderId);
            }

            var payments = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Select(p => new PaymentDto
                {
                    Id = p.Id.ToString(),
                    OrderId = p.OrderId,
                    CustomerId = p.CustomerId,
                    Amount = p.Amount,
                    Currency = p.Currency,
                    PaymentMethod = p.PaymentMethod,
                    PaymentProvider = p.Provider,
                    Status = p.Status,
                    ProviderTransactionId = p.ProviderTransactionId,
                    FailureReason = p.FailureReason,
                    Description = p.Description,
                    CreatedAt = p.CreatedAt,
                    Refunds = p.Refunds.Select(r => new RefundDto
                    {
                        Id = r.Id.ToString(),
                        Amount = r.Amount,
                        Currency = r.Currency,
                        Status = r.Status,
                        Reason = r.Reason,
                        ProviderRefundId = r.ProviderRefundId,
                        FailureReason = r.FailureReason,
                        CreatedAt = r.CreatedAt
                    }).ToList()
                })
                .ToListAsync();

            _logger.LogInformation("Found {Count} payments [CorrelationId: {CorrelationId}]", 
                payments.Count, correlationId);

            return payments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments [CorrelationId: {CorrelationId}]", correlationId);
            return new List<PaymentDto>();
        }
    }
}
