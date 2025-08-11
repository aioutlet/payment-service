using PaymentService.Models.DTOs;
using PaymentService.Models.Entities;

namespace PaymentService.Services.Providers;

/// <summary>
/// Interface for payment providers
/// </summary>
public interface IPaymentProvider
{
    string ProviderName { get; }
    List<string> SupportedPaymentMethods { get; }
    bool IsEnabled { get; }
    
    Task<PaymentProviderResult> ProcessPaymentAsync(ProcessPaymentDto request, string correlationId);
    Task<RefundProviderResult> ProcessRefundAsync(Payment payment, decimal amount, string reason, string correlationId);
    Task<PaymentMethodResult> SavePaymentMethodAsync(SavePaymentMethodDto request, string correlationId);
    Task<bool> DeletePaymentMethodAsync(string providerTokenId, string correlationId);
}

/// <summary>
/// Payment provider factory interface
/// </summary>
public interface IPaymentProviderFactory
{
    IPaymentProvider GetProvider(string providerName);
    IPaymentProvider GetDefaultProvider();
    List<IPaymentProvider> GetEnabledProviders();
    List<string> GetSupportedPaymentMethods(string providerName);
    Dictionary<string, List<string>> GetAllSupportedPaymentMethods();
    bool IsProviderSupported(string providerName);
    bool IsProviderEnabled(string providerName);
    IPaymentProvider GetProviderForPaymentMethod(string paymentMethod);
    List<string> GetAvailableProviders();
}

/// <summary>
/// Payment provider result
/// </summary>
public class PaymentProviderResult
{
    public bool IsSuccess { get; set; }
    public string? TransactionId { get; set; }
    public string? ProviderTransactionId { get; set; }
    public PaymentStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Refund provider result
/// </summary>
public class RefundProviderResult
{
    public bool IsSuccess { get; set; }
    public string? RefundId { get; set; }
    public string? ProviderRefundId { get; set; }
    public RefundStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Payment method provider result
/// </summary>
public class PaymentMethodResult
{
    public bool IsSuccess { get; set; }
    public string? ProviderTokenId { get; set; }
    public string? Last4Digits { get; set; }
    public string? Brand { get; set; }
    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }
    public string? FailureReason { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
