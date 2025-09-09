namespace PaymentService.Configuration;

/// <summary>
/// Stripe payment provider configuration
/// </summary>
public class StripeSettings
{
    public bool IsEnabled { get; set; } = true;
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2023-10-16";
    public string Environment { get; set; } = "sandbox";
    public bool SandboxMode { get; set; } = true;
    public List<string> SupportedMethods { get; set; } = new();
}
