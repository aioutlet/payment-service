namespace PaymentService.Models.DTOs;

/// <summary>
/// Result of saving a payment method
/// </summary>
public class SavePaymentMethodResultDto
{
    public bool IsSuccess { get; set; }
    public string MethodId { get; set; } = string.Empty;
    public string PaymentMethodId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string PaymentProvider { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public string? ProviderTokenId { get; set; }
    public string? CardLast4 { get; set; }
    public string? Last4Digits { get; set; }
    public string? CardBrand { get; set; }
    public string? Brand { get; set; }
    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
