namespace PaymentService.Configuration;

/// <summary>
/// Payment service settings
/// </summary>
public class PaymentSettings
{
    public const string SectionName = "PaymentSettings";
    
    public string DefaultCurrency { get; set; } = "USD";
    public decimal MaxPaymentAmount { get; set; } = 10000.00m;
    public decimal MinPaymentAmount { get; set; } = 0.50m;
    public int PaymentTimeout { get; set; } = 30;
    public int RefundTimeout { get; set; } = 7;
    public List<string> SupportedCurrencies { get; set; } = new();
}
