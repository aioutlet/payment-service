namespace PaymentService.Configuration;

/// <summary>
/// Logging configuration for Serilog and other logging providers
/// </summary>
public class LoggingSettings
{
    public string LogLevel { get; set; } = "Information";
    public bool EnableConsoleLogging { get; set; } = true;
    public bool EnableFileLogging { get; set; } = true;
    public string LogFilePath { get; set; } = "logs/payment-service-.log";
    public bool EnableDatabaseLogging { get; set; } = false;
    public string DatabaseLogConnectionString { get; set; } = string.Empty;
    public bool EnableSeqLogging { get; set; } = false;
    public string SeqServerUrl { get; set; } = string.Empty;
    public string SeqApiKey { get; set; } = string.Empty;
}
