using System.ComponentModel.DataAnnotations;

namespace ReconFlow.Api.DTOs;

public sealed record SavePaymentRequest(
    [property: Required, StringLength(120)] string TransactionReference,
    Guid CustomerId,
    [property: Range(typeof(decimal), "0.01", "9999999999999999")] decimal Amount,
    DateOnly PaymentDate,
    [property: Required, StringLength(500)] string Description);

public sealed class PaymentImportRequest
{
    [Required]
    public required IFormFile File { get; init; }
}
