namespace PaymentService.Models.DTOs;

/// <summary>
/// Payment method data transfer object for API responses
/// </summary>
public class PaymentMethodDto
{
    public string Id { get; set; } = string.Empty;
    public string MethodId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string PaymentProvider { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public string PaymentMethodType { get; set; } = string.Empty;
    public string? CardLast4 { get; set; }
    public string? Last4Digits { get; set; }
    public string? CardBrand { get; set; }
    public string? Brand { get; set; }
    public int? CardExpiryMonth { get; set; }
    public int? CardExpiryYear { get; set; }
    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }
    public string? BillingAddress { get; set; }
    public string? Email { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
