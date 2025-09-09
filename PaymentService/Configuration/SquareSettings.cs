namespace PaymentService.Configuration;

/// <summary>
/// Square payment provider configuration
/// </summary>
public class SquareSettings
{
    public string ApplicationId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public string Environment { get; set; } = "sandbox"; // sandbox or production
    public string WebhookSignatureKey { get; set; } = string.Empty;
    public bool SandboxMode { get; set; } = true;
}
