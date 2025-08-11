namespace PaymentService.Configuration;

/// <summary>
/// PayPal payment provider configuration
/// </summary>
public class PayPalSettings
{
    public bool IsEnabled { get; set; } = true;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.sandbox.paypal.com"; // sandbox
    public string Environment { get; set; } = "sandbox";
    public string WebhookId { get; set; } = string.Empty;
    public bool SandboxMode { get; set; } = true;
    public bool IsSandbox => Environment.Equals("sandbox", StringComparison.OrdinalIgnoreCase);
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public List<string> SupportedMethods { get; set; } = new();
}
