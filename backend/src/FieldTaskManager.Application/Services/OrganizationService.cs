using FieldTaskManager.Application.Common;
using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Application.Interfaces;
using FieldTaskManager.Application.Mapping;
using FieldTaskManager.Domain.Repositories;

namespace FieldTaskManager.Application.Services;

public sealed class OrganizationService(IUnitOfWork unitOfWork) : IOrganizationService
{
    public async Task<Result<IReadOnlyList<OrganizationDto>>> ListAsync(CurrentUser currentUser, CancellationToken ct = default)
    {
        var canManage = EnsureSuperAdmin(currentUser);
        if (canManage.IsFailure)
        {
            return Result.Failure<IReadOnlyList<OrganizationDto>>(canManage.Error);
        }

        var organizations = await unitOfWork.Organizations.ListAsync(ct);
        return Result.Success<IReadOnlyList<OrganizationDto>>(organizations.OrderBy(o => o.Name).Select(o => o.ToDto()).ToList());
    }

    public async Task<Result<OrganizationDto>> SetAccessAsync(
        Guid id,
        UpdateOrganizationAccessRequest request,
        CurrentUser currentUser,
        CancellationToken ct = default)
    {
        var canManage = EnsureSuperAdmin(currentUser);
        if (canManage.IsFailure)
        {
            return Result.Failure<OrganizationDto>(canManage.Error);
        }

        var organization = await unitOfWork.Organizations.GetByIdAsync(id, ct);
        if (organization is null)
        {
            return Result.Failure<OrganizationDto>(Error.NotFound($"Organization '{id}' was not found."));
        }

        organization.IsActive = request.IsActive;
        organization.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(organization.ToDto());
    }

    private static Result EnsureSuperAdmin(CurrentUser currentUser)
    {
        if (!currentUser.IsSuperAdmin)
        {
            return Result.Failure(Error.Forbidden("Only super administrators can perform this action."));
        }

        return Result.Success();
    }
}
