namespace ReconFlow.Application.Contracts;

public sealed record PaymentModel(
    Guid Id,
    string TransactionReference,
    Guid CustomerId,
    string CustomerName,
    decimal Amount,
    DateOnly PaymentDate,
    string Description,
    DateTimeOffset CreatedAt,
    bool IsReconciled);

public sealed record SavePayment(
    string TransactionReference,
    Guid CustomerId,
    decimal Amount,
    DateOnly PaymentDate,
    string Description);
