using PaymentService.Models.DTOs;

namespace PaymentService.Services;

/// <summary>
/// Interface for payment service operations
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Process a payment
    /// </summary>
    Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentDto request);

    /// <summary>
    /// Process a refund for an existing payment
    /// </summary>
    Task<RefundResultDto> ProcessRefundAsync(ProcessRefundDto request);

    /// <summary>
    /// Save a payment method for future use
    /// </summary>
    Task<SavePaymentMethodResultDto> SavePaymentMethodAsync(SavePaymentMethodDto request);

    /// <summary>
    /// Delete a saved payment method
    /// </summary>
    Task<bool> DeletePaymentMethodAsync(Guid paymentMethodId);

    /// <summary>
    /// Get all payment methods for a customer
    /// </summary>
    Task<List<PaymentMethodDto>> GetPaymentMethodsAsync(string customerId);

    /// <summary>
    /// Get payment details by ID
    /// </summary>
    Task<PaymentDto?> GetPaymentAsync(Guid paymentId);

    /// <summary>
    /// Get payments with optional filtering
    /// </summary>
    Task<List<PaymentDto>> GetPaymentsAsync(string? customerId = null, string? orderId = null, int skip = 0, int take = 50);
}
