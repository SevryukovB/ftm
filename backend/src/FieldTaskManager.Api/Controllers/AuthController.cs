using FieldTaskManager.Api.Extensions;
using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FieldTaskManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct) =>
        (await authService.RegisterAsync(request, ct)).ToActionResult(this);

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct) =>
        (await authService.LoginAsync(request, ct)).ToActionResult(this);
}
