using System.ComponentModel.DataAnnotations;

namespace ReconFlow.Api.DTOs;

public sealed record SaveCustomerRequest(
    [property: Required, StringLength(200)] string Name,
    [property: Required, EmailAddress, StringLength(320)] string Email,
    [property: Required, StringLength(80)] string AccountReference);
