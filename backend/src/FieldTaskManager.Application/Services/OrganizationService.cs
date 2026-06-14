using FieldTaskManager.Application.Common;
using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Application.Exceptions;
using FieldTaskManager.Application.Interfaces;
using FieldTaskManager.Application.Mapping;
using FieldTaskManager.Domain.Repositories;

namespace FieldTaskManager.Application.Services;

public sealed class OrganizationService(IUnitOfWork unitOfWork) : IOrganizationService
{
    public async Task<IReadOnlyList<OrganizationDto>> ListAsync(CurrentUser currentUser, CancellationToken ct = default)
    {
        EnsureSuperAdmin(currentUser);
        var organizations = await unitOfWork.Organizations.ListAsync(ct);
        return organizations.OrderBy(o => o.Name).Select(o => o.ToDto()).ToList();
    }

    public async Task<OrganizationDto> SetAccessAsync(
        Guid id,
        UpdateOrganizationAccessRequest request,
        CurrentUser currentUser,
        CancellationToken ct = default)
    {
        EnsureSuperAdmin(currentUser);
        var organization = await unitOfWork.Organizations.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Organization '{id}' was not found.");

        organization.IsActive = request.IsActive;
        organization.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);
        return organization.ToDto();
    }

    private static void EnsureSuperAdmin(CurrentUser currentUser)
    {
        if (!currentUser.IsSuperAdmin)
        {
            throw new ForbiddenException("Only super administrators can perform this action.");
        }
    }
}
