namespace PaymentService.Configuration;

/// <summary>
/// Main payment service configuration that aggregates all settings
/// </summary>
public class PaymentServiceSettings
{
    public StripeSettings Stripe { get; set; } = new();
    public PayPalSettings PayPal { get; set; } = new();
    public SquareSettings Square { get; set; } = new();
    public DatabaseSettings Database { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
    
    // General payment service settings
    public string DefaultPaymentProvider { get; set; } = "stripe";
    public string DefaultCurrency { get; set; } = "USD";
    public decimal MaxPaymentAmount { get; set; } = 10000.00m;
    public decimal MinPaymentAmount { get; set; } = 0.01m;
    public int PaymentTimeoutSeconds { get; set; } = 30;
    public bool EnableWebhooks { get; set; } = true;
    public string WebhookEndpoint { get; set; } = "/api/webhooks";
    public bool EnableMetrics { get; set; } = true;
    public bool EnableHealthChecks { get; set; } = true;
}
