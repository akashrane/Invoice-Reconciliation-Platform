using ReconFlow.Core.Enums;

namespace ReconFlow.Application.Contracts;

public sealed record ReconciliationModel(
    Guid Id,
    Guid PaymentId,
    string PaymentReference,
    Guid? InvoiceId,
    string? InvoiceNumber,
    string CustomerName,
    decimal Amount,
    int MatchScore,
    ReconciliationStatus MatchStatus,
    IReadOnlyList<string> Reasons,
    string? ReviewedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewedAt,
    IReadOnlyList<AuditModel> AuditTrail);

public sealed record AuditModel(
    Guid Id,
    string User,
    string Action,
    ReconciliationStatus PreviousStatus,
    ReconciliationStatus NewStatus,
    string Reason,
    DateTimeOffset CreatedAt);
