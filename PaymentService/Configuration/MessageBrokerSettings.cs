namespace PaymentService.Configuration;

/// <summary>
/// Message broker settings
/// </summary>
public class MessageBrokerSettings
{
    public const string SectionName = "MessageBroker";
    
    public string Type { get; set; } = "RabbitMQ";
    public RabbitMQSettings RabbitMQ { get; set; } = new();
    public TopicsSettings Topics { get; set; } = new();
}
