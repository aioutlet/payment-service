namespace PaymentService.Models.Entities;

/// <summary>
/// Payment status enumeration
/// </summary>
public enum PaymentStatus
{
    Pending = 1,
    Processing = 2,
    Succeeded = 3,
    Failed = 4,
    Cancelled = 5,
    PartiallyRefunded = 6,
    FullyRefunded = 7
}
