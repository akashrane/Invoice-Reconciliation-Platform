using ReconFlow.Core.Enums;

namespace ReconFlow.Application.Contracts;

public sealed record AuthResult(Guid UserId, string Email, UserRole Role, string Token);
public sealed record RegisterUser(string Email, string Password);
