namespace ReconFlow.Application.Contracts;

public sealed record CustomerModel(
    Guid Id,
    string Name,
    string Email,
    string AccountReference,
    DateTimeOffset CreatedAt);

public sealed record SaveCustomer(string Name, string Email, string AccountReference);
