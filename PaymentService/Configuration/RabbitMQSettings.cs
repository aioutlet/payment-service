namespace PaymentService.Configuration;

/// <summary>
/// RabbitMQ configuration
/// </summary>
public class RabbitMQSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Exchange { get; set; } = "payments.exchange";
    public string ExchangeType { get; set; } = "topic";
    public bool PublisherConfirms { get; set; } = true;
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelay { get; set; } = 1000;
}
