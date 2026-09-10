using System.ComponentModel.DataAnnotations;
using ReconFlow.Core.Enums;

namespace ReconFlow.Api.DTOs;

public sealed record SaveInvoiceRequest(
    [property: Required, StringLength(80)] string InvoiceNumber,
    Guid CustomerId,
    [property: Range(typeof(decimal), "0.01", "9999999999999999")] decimal Amount,
    DateOnly IssueDate,
    DateOnly DueDate,
    InvoiceStatus Status = InvoiceStatus.Open);
