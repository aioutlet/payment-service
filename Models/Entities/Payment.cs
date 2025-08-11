using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.Models.Entities;

/// <summary>
/// Payment entity for SQL Server
/// </summary>
[Table("Payments")]
[Index(nameof(OrderId), IsUnique = true)]
[Index(nameof(CorrelationId))]
[Index(nameof(CustomerId))]
[Index(nameof(Status))]
public class Payment
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string OrderId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string CustomerId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string CorrelationId { get; set; } = string.Empty;
    
    [Column(TypeName = "money")]
    public decimal Amount { get; set; }
    
    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = "USD";
    
    [Required]
    public PaymentStatus Status { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Provider { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string? TransactionId { get; set; }
    
    [StringLength(200)]
    public string? ProviderTransactionId { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
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
    
    // Navigation properties
    public virtual ICollection<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();
    public virtual ICollection<PaymentRefund> Refunds { get; set; } = new List<PaymentRefund>();
}
