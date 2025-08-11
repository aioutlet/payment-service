using System.ComponentModel.DataAnnotations;

namespace PaymentService.Models.DTOs;

/// <summary>
/// Payment method details embedded within payment requests
/// </summary>
public class PaymentMethodDetailsDto
{
    public string? Token { get; set; }
    public string? TokenId { get; set; }
    public string? CardLast4 { get; set; }
    public string? CardBrand { get; set; }
    public int? CardExpiryMonth { get; set; }
    public int? CardExpiryYear { get; set; }
    public string? BillingAddress { get; set; }
    public string? Email { get; set; }
    public CardDetailsDto? Card { get; set; }
}

/// <summary>
/// Card details for payment method
/// </summary>
public class CardDetailsDto
{
    public string? Number { get; set; }
    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }
    public string? Cvc { get; set; }
    public string? Last4 { get; set; }
    public string? Brand { get; set; }
    public string? HolderName { get; set; }
}
