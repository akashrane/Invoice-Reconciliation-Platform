using ReconFlow.Core.Enums;

namespace ReconFlow.Core.Entities;

public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReconciliationId { get; set; }
    public Reconciliation Reconciliation { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public required string Action { get; set; }
    public ReconciliationStatus PreviousStatus { get; set; }
    public ReconciliationStatus NewStatus { get; set; }
    public Guid PaymentId { get; set; }
    public Guid? InvoiceId { get; set; }
    public required string Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
