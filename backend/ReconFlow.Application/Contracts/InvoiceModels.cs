using ReconFlow.Core.Enums;

namespace ReconFlow.Application.Contracts;

public sealed record InvoiceModel(
    Guid Id,
    string InvoiceNumber,
    Guid CustomerId,
    string CustomerName,
    decimal Amount,
    DateOnly IssueDate,
    DateOnly DueDate,
    InvoiceStatus Status,
    DateTimeOffset CreatedAt);

public sealed record SaveInvoice(
    string InvoiceNumber,
    Guid CustomerId,
    decimal Amount,
    DateOnly IssueDate,
    DateOnly DueDate,
    InvoiceStatus Status);
