using PaymentService.Models.Entities;

namespace PaymentService.Models.DTOs;

/// <summary>
/// Response after processing a refund
/// </summary>
public class RefundResponseDto
{
    public string RefundId { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public RefundStatus Status { get; set; }
    public string? ProviderRefundId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Reason { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public string? ErrorMessage { get; set; }
}
