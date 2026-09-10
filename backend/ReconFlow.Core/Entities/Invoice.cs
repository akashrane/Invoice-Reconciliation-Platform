using ReconFlow.Core.Enums;

namespace ReconFlow.Core.Entities;

public sealed class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string InvoiceNumber { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Open;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Reconciliation> Reconciliations { get; set; } = [];
}
