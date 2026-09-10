using Microsoft.EntityFrameworkCore;
using ReconFlow.Application.Abstractions;
using ReconFlow.Application.Contracts;
using ReconFlow.Application.Exceptions;
using ReconFlow.Core.Entities;

namespace ReconFlow.Application.Services;

public sealed class AuthService(IReconFlowDbContext db, IPasswordService passwords, ITokenService tokens)
{
    public async Task<AuthResult> RegisterAsync(RegisterUser request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
            throw new ConflictException("EMAIL_ALREADY_REGISTERED", "An account with this email already exists.");
        ValidatePassword(request.Password);

        var user = new User { Email = email, PasswordHash = passwords.Hash(request.Password) };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return Result(user);
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == normalized, cancellationToken);
        if (user is null || !passwords.Verify(user.PasswordHash, password))
            throw new AppException("INVALID_CREDENTIALS", "Email or password is incorrect.", 401);
        return Result(user);
    }

    private AuthResult Result(User user) => new(user.Id, user.Email, user.Role, tokens.Create(user));

    private static void ValidatePassword(string password)
    {
        if (password.Length < 10 || !password.Any(char.IsUpper) || !password.Any(char.IsLower)
            || !password.Any(char.IsDigit) || password.All(char.IsLetterOrDigit))
            throw new BusinessRuleException("WEAK_PASSWORD",
                "Password must be at least 10 characters and include uppercase, lowercase, number, and symbol characters.");
    }
}
