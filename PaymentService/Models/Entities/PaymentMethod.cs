using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.Models.Entities;

/// <summary>
/// Payment method entity
/// </summary>
[Table("PaymentMethods")]
[Index(nameof(CustomerId))]
public class PaymentMethod
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string CustomerId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty; // visa, mastercard, amex, paypal, etc.
    
    [StringLength(100)]
    public string? DisplayName { get; set; }
    
    [StringLength(4)]
    public string? Last4Digits { get; set; }
    
    [StringLength(50)]
    public string? Brand { get; set; }
    
    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }
    
    [Required]
    [StringLength(200)]
    public string ProviderTokenId { get; set; } = string.Empty; // Token from payment provider
    
    [Required]
    [StringLength(50)]
    public string Provider { get; set; } = string.Empty; // stripe, paypal, square
    
    [Column(TypeName = "nvarchar(max)")]
    public string? Metadata { get; set; }
    
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(100)]
    public string CreatedBy { get; set; } = "System";
    
    [StringLength(100)]
    public string UpdatedBy { get; set; } = "System";
}
