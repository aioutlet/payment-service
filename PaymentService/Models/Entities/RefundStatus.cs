namespace PaymentService.Models.Entities;

/// <summary>
/// Refund status enumeration
/// </summary>
public enum RefundStatus
{
    Pending = 1,
    Processing = 2,
    Succeeded = 3,
    Failed = 4,
    Cancelled = 5
}
