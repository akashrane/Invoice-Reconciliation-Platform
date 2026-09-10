using System.ComponentModel.DataAnnotations;

namespace ReconFlow.Api.DTOs;

public sealed record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public sealed record RegisterRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required, MinLength(10)] string Password);
