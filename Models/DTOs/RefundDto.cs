using System.ComponentModel.DataAnnotations;
using PaymentService.Models.Entities;

namespace PaymentService.Models.DTOs;

/// <summary>
/// Request to refund a payment
/// </summary>
public class RefundDto
{
    public string? Id { get; set; }
    
    [Required]
    public string PaymentId { get; set; } = string.Empty;
    
    [Required]
    [Range(0.01, 10000.00)]
    public decimal Amount { get; set; }
    
    public string? Currency { get; set; }
    public RefundStatus? Status { get; set; }
    public string? ProviderRefundId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? FailureReason { get; set; }
    public string? Reason { get; set; }
    
    public Dictionary<string, object>? Metadata { get; set; }
}
