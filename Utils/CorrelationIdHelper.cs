namespace PaymentService.Utils;

/// <summary>
/// Helper for managing correlation IDs
/// </summary>
public static class CorrelationIdHelper
{
    private static readonly AsyncLocal<string> _correlationId = new();

    /// <summary>
    /// Gets or sets the current correlation ID
    /// </summary>
    public static string CorrelationId
    {
        get => _correlationId.Value ?? Guid.NewGuid().ToString();
        set => _correlationId.Value = value;
    }

    /// <summary>
    /// Gets the current correlation ID or creates a new one
    /// </summary>
    public static string GetCorrelationId()
    {
        return CorrelationId;
    }

    /// <summary>
    /// Sets a new correlation ID
    /// </summary>
    public static void SetCorrelationId(string correlationId)
    {
        CorrelationId = correlationId;
    }

    /// <summary>
    /// Generates a new correlation ID and sets it as current
    /// </summary>
    public static string GenerateCorrelationId()
    {
        var correlationId = Guid.NewGuid().ToString();
        SetCorrelationId(correlationId);
        return correlationId;
    }
}
