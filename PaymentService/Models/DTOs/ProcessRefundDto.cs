using System.ComponentModel.DataAnnotations;

namespace PaymentService.Models.DTOs;

/// <summary>
/// Request to process a refund
/// </summary>
public class ProcessRefundDto
{
    [Required]
    public string PaymentId { get; set; } = string.Empty;
    
    [Required]
    [Range(0.01, 10000.00)]
    public decimal Amount { get; set; }
    
    public string? Reason { get; set; }
    
    public Dictionary<string, object>? Metadata { get; set; }
}
