using System.ComponentModel.DataAnnotations;

namespace PaymentService.Models.DTOs;

/// <summary>
/// Request to process a payment
/// </summary>
public class ProcessPaymentDto
{
    [Required]
    public string OrderId { get; set; } = string.Empty;
    
    [Required]
    public string CustomerId { get; set; } = string.Empty;
    
    [Required]
    [Range(0.01, 10000.00)]
    public decimal Amount { get; set; }
    
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "USD";
    
    [Required]
    public string PaymentMethod { get; set; } = string.Empty; // visa, mastercard, amex, paypal
    
    public string? PaymentProvider { get; set; } // stripe, paypal, square
    
    public PaymentMethodDetailsDto? PaymentMethodDetails { get; set; }
    
    public string? Description { get; set; }
    
    public Dictionary<string, object>? Metadata { get; set; }
}
