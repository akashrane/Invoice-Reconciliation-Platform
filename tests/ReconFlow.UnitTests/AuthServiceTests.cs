using Microsoft.EntityFrameworkCore;
using ReconFlow.Application.Abstractions;
using ReconFlow.Application.Contracts;
using ReconFlow.Application.Exceptions;
using ReconFlow.Application.Services;
using ReconFlow.Core.Entities;
using ReconFlow.Infrastructure.Auth;
using ReconFlow.Infrastructure.Data;

namespace ReconFlow.UnitTests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task Registration_hashes_password_and_login_returns_token()
    {
        await using var db = NewDb();
        var service = new AuthService(db, new AspNetPasswordService(), new FakeTokenService());

        var registered = await service.RegisterAsync(new RegisterUser("Analyst@Example.com", "Secure!Pass1"), default);
        var stored = await db.Users.SingleAsync();
        var loggedIn = await service.LoginAsync("analyst@example.com", "Secure!Pass1", default);

        Assert.NotEqual("Secure!Pass1", stored.PasswordHash);
        Assert.Equal("analyst@example.com", registered.Email);
        Assert.Equal("test-token", loggedIn.Token);
    }

    [Fact]
    public async Task Invalid_credentials_return_generic_unauthorized_error()
    {
        await using var db = NewDb();
        var service = new AuthService(db, new AspNetPasswordService(), new FakeTokenService());

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.LoginAsync("missing@example.com", "Wrong!Pass1", default));

        Assert.Equal(401, exception.StatusCode);
        Assert.Equal("INVALID_CREDENTIALS", exception.Code);
    }

    private static ReconFlowDbContext NewDb() => new(new DbContextOptionsBuilder<ReconFlowDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private sealed class FakeTokenService : ITokenService
    {
        public string Create(User user) => "test-token";
    }
}
