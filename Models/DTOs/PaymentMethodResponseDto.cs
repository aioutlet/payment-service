using PaymentService.Models.Entities;

namespace PaymentService.Models.DTOs;

/// <summary>
/// Response when retrieving saved payment methods
/// </summary>
public class PaymentMethodResponseDto
{
    public string MethodId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string PaymentProvider { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public string? CardLast4 { get; set; }
    public string? CardBrand { get; set; }
    public int? CardExpiryMonth { get; set; }
    public int? CardExpiryYear { get; set; }
    public string? BillingAddress { get; set; }
    public string? Email { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
