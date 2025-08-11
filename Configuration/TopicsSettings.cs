namespace PaymentService.Configuration;

/// <summary>
/// Message topics configuration
/// </summary>
public class TopicsSettings
{
    public string PaymentProcessed { get; set; } = "payment.processed";
    public string PaymentFailed { get; set; } = "payment.failed";
    public string PaymentRefunded { get; set; } = "payment.refunded";
}
