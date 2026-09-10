using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReconFlow.Api.DTOs;
using ReconFlow.Application.Contracts;
using ReconFlow.Application.Services;

namespace ReconFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(AuthService service) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResult>> Register(RegisterRequest request, CancellationToken ct) =>
        Ok(await service.RegisterAsync(new RegisterUser(request.Email, request.Password), ct));

    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await service.LoginAsync(request.Email, request.Password, ct));
}
