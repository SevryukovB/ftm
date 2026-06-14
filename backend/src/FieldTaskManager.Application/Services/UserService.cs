using FieldTaskManager.Application.Common;
using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Application.Interfaces;
using FieldTaskManager.Application.Mapping;
using FieldTaskManager.Domain.Entities;
using FieldTaskManager.Domain.Enums;
using FieldTaskManager.Domain.Repositories;
using Microsoft.AspNetCore.Identity;

namespace FieldTaskManager.Application.Services;

public sealed class UserService(IUnitOfWork unitOfWork, IPasswordHasher<User> passwordHasher) : IUserService
{
    public async Task<Result<IReadOnlyList<UserDto>>> ListAsync(CurrentUser currentUser, CancellationToken ct = default)
    {
        var canManage = EnsureOrgAdmin(currentUser);
        if (canManage.IsFailure)
        {
            return Result.Failure<IReadOnlyList<UserDto>>(canManage.Error);
        }

        var organizationResult = RequireOrganization(currentUser);
        if (organizationResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<UserDto>>(organizationResult.Error);
        }

        var organizationId = organizationResult.Value;
        var users = await unitOfWork.Users.ListByOrganizationAsync(organizationId, ct);
        return Result.Success<IReadOnlyList<UserDto>>(users.Select(w => w.ToDto()).ToList());
    }

    public async Task<Result<IReadOnlyList<UserDto>>> ListWorkersAsync(CurrentUser currentUser, CancellationToken ct = default)
    {
        var canManage = EnsureOrgAdmin(currentUser);
        if (canManage.IsFailure)
        {
            return Result.Failure<IReadOnlyList<UserDto>>(canManage.Error);
        }

        var organizationResult = RequireOrganization(currentUser);
        if (organizationResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<UserDto>>(organizationResult.Error);
        }

        var organizationId = organizationResult.Value;
        var workers = await unitOfWork.Users.ListByRoleAsync(organizationId, UserRole.Worker, ct);
        return Result.Success<IReadOnlyList<UserDto>>(workers.Select(w => w.ToDto()).ToList());
    }

    public async Task<Result<UserDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var user = await unitOfWork.Users.GetByIdAsync(id, ct);
        return user is null
            ? Result.Failure<UserDto>(Error.NotFound($"User '{id}' was not found."))
            : Result.Success(user.ToDto());
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CurrentUser currentUser, CancellationToken ct = default)
    {
        var canManage = EnsureOrgAdmin(currentUser);
        if (canManage.IsFailure)
        {
            return Result.Failure<UserDto>(canManage.Error);
        }

        var organizationResult = RequireOrganization(currentUser);
        if (organizationResult.IsFailure)
        {
            return Result.Failure<UserDto>(organizationResult.Error);
        }

        var organizationId = organizationResult.Value;
        var email = request.Email.Trim().ToLowerInvariant();

        if (await unitOfWork.Users.ExistsByEmailAsync(email, ct))
        {
            return Result.Failure<UserDto>(Error.Conflict($"User with email '{email}' already exists."));
        }

        var roleResult = ParseOrganizationRole(request.Role);
        if (roleResult.IsFailure)
        {
            return Result.Failure<UserDto>(roleResult.Error);
        }

        var role = roleResult.Value;
        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            Role = role,
            OrganizationId = organizationId,
            IsActive = true
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        unitOfWork.Users.Add(user);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success((await unitOfWork.Users.GetByIdAsync(user.Id, ct))!.ToDto());
    }

    public async Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request, CurrentUser currentUser, CancellationToken ct = default)
    {
        var canManage = EnsureOrgAdmin(currentUser);
        if (canManage.IsFailure)
        {
            return Result.Failure<UserDto>(canManage.Error);
        }

        var organizationResult = RequireOrganization(currentUser);
        if (organizationResult.IsFailure)
        {
            return Result.Failure<UserDto>(organizationResult.Error);
        }

        var organizationId = organizationResult.Value;
        var userResult = await GetOrganizationUserAsync(id, organizationId, ct);
        if (userResult.IsFailure)
        {
            return Result.Failure<UserDto>(userResult.Error);
        }

        var user = userResult.Value;

        if (user.Id == currentUser.Id && !request.IsActive)
        {
            return Result.Failure<UserDto>(Error.BadRequest("Administrators cannot deactivate themselves."));
        }

        var roleResult = ParseOrganizationRole(request.Role);
        if (roleResult.IsFailure)
        {
            return Result.Failure<UserDto>(roleResult.Error);
        }

        user.FullName = request.FullName.Trim();
        user.Role = roleResult.Value;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(user.ToDto());
    }

    public async Task<Result> DeactivateAsync(Guid id, CurrentUser currentUser, CancellationToken ct = default)
    {
        var canManage = EnsureOrgAdmin(currentUser);
        if (canManage.IsFailure)
        {
            return canManage;
        }

        var organizationResult = RequireOrganization(currentUser);
        if (organizationResult.IsFailure)
        {
            return Result.Failure(organizationResult.Error);
        }

        var organizationId = organizationResult.Value;

        if (id == currentUser.Id)
        {
            return Result.Failure(Error.BadRequest("Administrators cannot deactivate themselves."));
        }

        var userResult = await GetOrganizationUserAsync(id, organizationId, ct);
        if (userResult.IsFailure)
        {
            return Result.Failure(userResult.Error);
        }

        var user = userResult.Value;
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<Result<User>> GetOrganizationUserAsync(Guid id, Guid organizationId, CancellationToken ct)
    {
        var user = await unitOfWork.Users.GetByIdAsync(id, ct);
        if (user is null)
        {
            return Result.Failure<User>(Error.NotFound($"User '{id}' was not found."));
        }

        if (user.OrganizationId != organizationId)
        {
            return Result.Failure<User>(Error.NotFound($"User '{id}' was not found."));
        }

        return Result.Success(user);
    }

    private static Result EnsureOrgAdmin(CurrentUser user)
    {
        if (!user.IsOrgAdmin)
        {
            return Result.Failure(Error.Forbidden("Only organization administrators can perform this action."));
        }

        return Result.Success();
    }

    private static Result<Guid> RequireOrganization(CurrentUser user) =>
        user.OrganizationId is Guid organizationId
            ? Result.Success(organizationId)
            : Result.Failure<Guid>(Error.Forbidden("Current user is not linked to an organization."));

    private static Result<UserRole> ParseOrganizationRole(string value)
    {
        if (!Enum.TryParse<UserRole>(value, ignoreCase: true, out var role) ||
            role is UserRole.SuperAdmin)
        {
            return Result.Failure<UserRole>(Error.BadRequest("User role must be OrgAdmin or Worker."));
        }

        return Result.Success(role);
    }
}
