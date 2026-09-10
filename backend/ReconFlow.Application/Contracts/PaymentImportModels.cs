namespace ReconFlow.Application.Contracts;

public sealed record PaymentImportError(int Row, string? Reference, string Message);

public sealed record PaymentImportResult(
    int Uploaded,
    int Matched,
    int Review,
    int Unmatched,
    int Failed,
    IReadOnlyList<PaymentImportError> Errors);
