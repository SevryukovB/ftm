using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Application.Exceptions;
using FieldTaskManager.Application.Interfaces;
using FieldTaskManager.Application.Mapping;
using FieldTaskManager.Domain.Entities;
using FieldTaskManager.Domain.Enums;
using FieldTaskManager.Domain.Repositories;
using Microsoft.AspNetCore.Identity;

namespace FieldTaskManager.Application.Services;

public sealed class AuthService(
    IUnitOfWork unitOfWork,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await unitOfWork.Users.ExistsByEmailAsync(email, ct))
        {
            throw new ConflictException($"User with email '{email}' already exists.");
        }

        // Everyone who self-registers gets the Worker role by design.
        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            Role = UserRole.Worker
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        unitOfWork.Users.Add(user);
        await unitOfWork.SaveChangesAsync(ct);

        return new AuthResponse(tokenService.CreateToken(user), user.ToDto());
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await unitOfWork.Users.GetByEmailAsync(email, ct)
            ?? throw new UnauthorizedException("Invalid email or password.");

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        return new AuthResponse(tokenService.CreateToken(user), user.ToDto());
    }
}
