using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Application.Common;
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
    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await unitOfWork.Users.ExistsByEmailAsync(email, ct))
        {
            return Result.Failure<AuthResponse>(Error.Conflict($"User with email '{email}' already exists."));
        }

        var organization = new Organization
        {
            Name = request.OrganizationName.Trim()
        };

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            Role = UserRole.OrgAdmin,
            Organization = organization,
            OrganizationId = organization.Id
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        unitOfWork.Organizations.Add(organization);
        unitOfWork.Users.Add(user);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new AuthResponse(tokenService.CreateToken(user), user.ToDto()));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await unitOfWork.Users.GetByEmailWithOrganizationAsync(email, ct);
        if (user is null)
        {
            return Result.Failure<AuthResponse>(Error.Unauthorized("Invalid email or password."));
        }

        if (!user.IsActive || user.Organization?.IsActive == false)
        {
            return Result.Failure<AuthResponse>(Error.Unauthorized("User or organization access is disabled."));
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Result.Failure<AuthResponse>(Error.Unauthorized("Invalid email or password."));
        }

        return Result.Success(new AuthResponse(tokenService.CreateToken(user), user.ToDto()));
    }
}
