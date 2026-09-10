namespace ReconFlow.Core.Entities;

public sealed class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string AccountReference { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}
