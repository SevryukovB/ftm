using FieldTaskManager.Application.Common;
using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Application.Exceptions;
using FieldTaskManager.Application.Interfaces;
using FieldTaskManager.Application.Mapping;
using FieldTaskManager.Domain.Entities;
using FieldTaskManager.Domain.Enums;
using FieldTaskManager.Domain.Repositories;
using Microsoft.AspNetCore.Identity;

namespace FieldTaskManager.Application.Services;

public sealed class UserService(IUnitOfWork unitOfWork, IPasswordHasher<User> passwordHasher) : IUserService
{
    public async Task<IReadOnlyList<UserDto>> ListAsync(CurrentUser currentUser, CancellationToken ct = default)
    {
        EnsureOrgAdmin(currentUser);
        var organizationId = RequireOrganization(currentUser);
        var users = await unitOfWork.Users.ListByOrganizationAsync(organizationId, ct);
        return users.Select(w => w.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<UserDto>> ListWorkersAsync(CurrentUser currentUser, CancellationToken ct = default)
    {
        EnsureOrgAdmin(currentUser);
        var organizationId = RequireOrganization(currentUser);
        var workers = await unitOfWork.Users.ListByRoleAsync(organizationId, UserRole.Worker, ct);
        return workers.Select(w => w.ToDto()).ToList();
    }

    public async Task<UserDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var user = await unitOfWork.Users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"User '{id}' was not found.");
        return user.ToDto();
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CurrentUser currentUser, CancellationToken ct = default)
    {
        EnsureOrgAdmin(currentUser);
        var organizationId = RequireOrganization(currentUser);
        var email = request.Email.Trim().ToLowerInvariant();

        if (await unitOfWork.Users.ExistsByEmailAsync(email, ct))
        {
            throw new ConflictException($"User with email '{email}' already exists.");
        }

        var role = ParseOrganizationRole(request.Role);
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
        return (await unitOfWork.Users.GetByIdAsync(user.Id, ct))!.ToDto();
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CurrentUser currentUser, CancellationToken ct = default)
    {
        EnsureOrgAdmin(currentUser);
        var organizationId = RequireOrganization(currentUser);
        var user = await GetOrganizationUserOrThrowAsync(id, organizationId, ct);

        if (user.Id == currentUser.Id && !request.IsActive)
        {
            throw new BadRequestException("Administrators cannot deactivate themselves.");
        }

        user.FullName = request.FullName.Trim();
        user.Role = ParseOrganizationRole(request.Role);
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(ct);
        return user.ToDto();
    }

    public async Task DeactivateAsync(Guid id, CurrentUser currentUser, CancellationToken ct = default)
    {
        EnsureOrgAdmin(currentUser);
        var organizationId = RequireOrganization(currentUser);

        if (id == currentUser.Id)
        {
            throw new BadRequestException("Administrators cannot deactivate themselves.");
        }

        var user = await GetOrganizationUserOrThrowAsync(id, organizationId, ct);
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<User> GetOrganizationUserOrThrowAsync(Guid id, Guid organizationId, CancellationToken ct)
    {
        var user = await unitOfWork.Users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"User '{id}' was not found.");

        if (user.OrganizationId != organizationId)
        {
            throw new NotFoundException($"User '{id}' was not found.");
        }

        return user;
    }

    private static void EnsureOrgAdmin(CurrentUser user)
    {
        if (!user.IsOrgAdmin)
        {
            throw new ForbiddenException("Only organization administrators can perform this action.");
        }
    }

    private static Guid RequireOrganization(CurrentUser user) =>
        user.OrganizationId ?? throw new ForbiddenException("Current user is not linked to an organization.");

    private static UserRole ParseOrganizationRole(string value)
    {
        if (!Enum.TryParse<UserRole>(value, ignoreCase: true, out var role) ||
            role is UserRole.SuperAdmin)
        {
            throw new BadRequestException("User role must be OrgAdmin or Worker.");
        }

        return role;
    }
}
