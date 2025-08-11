namespace PaymentService.Configuration;

/// <summary>
/// Security configuration for JWT and authentication
/// </summary>
public class SecuritySettings
{
    public string JwtSecret { get; set; } = string.Empty;
    public string JwtIssuer { get; set; } = string.Empty;
    public string JwtAudience { get; set; } = string.Empty;
    public int JwtExpirationMinutes { get; set; } = 60;
    public string EncryptionKey { get; set; } = string.Empty;
    public bool RequireHttps { get; set; } = true;
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
