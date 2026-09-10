using ReconFlow.Core.Enums;

namespace ReconFlow.Core.Entities;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.Analyst;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Reconciliation> ReviewedReconciliations { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
