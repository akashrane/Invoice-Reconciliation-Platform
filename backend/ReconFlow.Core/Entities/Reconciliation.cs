using ReconFlow.Core.Enums;

namespace ReconFlow.Core.Entities;

public sealed class Reconciliation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    public int MatchScore { get; set; }
    public ReconciliationStatus MatchStatus { get; set; }
    public required string Reason { get; set; }
    public Guid? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
