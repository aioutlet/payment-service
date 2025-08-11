using System.ComponentModel.DataAnnotations;

namespace PaymentService.Models.DTOs;

/// <summary>
/// Request to save a payment method
/// </summary>
public class SavePaymentMethodDto
{
    [Required]
    public string CustomerId { get; set; } = string.Empty;
    
    [Required]
    public string PaymentProvider { get; set; } = string.Empty; // stripe, paypal, square
    
    [Required]
    public string PaymentType { get; set; } = string.Empty; // visa, mastercard, amex, paypal
    
    public string PaymentMethodType { get; set; } = string.Empty; // card, bank_account, etc.
    
    [Required]
    public string ProviderToken { get; set; } = string.Empty;
    
    public string? CardLast4 { get; set; }
    public string? CardBrand { get; set; }
    public int? CardExpiryMonth { get; set; }
    public int? CardExpiryYear { get; set; }
    public string? BillingAddress { get; set; }
    public string? Email { get; set; }
    public bool IsDefault { get; set; }
    
    public PaymentMethodDetailsDto? PaymentMethodDetails { get; set; }
}
