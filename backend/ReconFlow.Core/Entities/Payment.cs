namespace ReconFlow.Core.Entities;

public sealed class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string TransactionReference { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateOnly PaymentDate { get; set; }
    public required string Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Reconciliation> Reconciliations { get; set; } = [];
}
