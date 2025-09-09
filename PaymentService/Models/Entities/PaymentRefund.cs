using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.Models.Entities;

/// <summary>
/// Payment refund entity
/// </summary>
[Table("PaymentRefunds")]
[Index(nameof(PaymentId))]
public class PaymentRefund
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid PaymentId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string CorrelationId { get; set; } = string.Empty;
    
    [Column(TypeName = "money")]
    public decimal Amount { get; set; }
    
    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = "USD";
    
    [Required]
    public RefundStatus Status { get; set; }
    
    [StringLength(200)]
    public string? RefundId { get; set; }
    
    [StringLength(200)]
    public string? ProviderRefundId { get; set; }
    
    [StringLength(500)]
    public string? Reason { get; set; }
    
    [Column(TypeName = "nvarchar(max)")]
    public string? Metadata { get; set; }
    
    [StringLength(1000)]
    public string? FailureReason { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(100)]
    public string CreatedBy { get; set; } = "System";
    
    [StringLength(100)]
    public string UpdatedBy { get; set; } = "System";
    
    // Navigation property
    [ForeignKey(nameof(PaymentId))]
    public virtual Payment Payment { get; set; } = null!;
}
