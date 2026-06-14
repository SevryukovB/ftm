using FieldTaskManager.Application.Common;
using FieldTaskManager.Application.Dtos;

namespace FieldTaskManager.Application.Interfaces;

public interface IOrganizationService
{
    Task<IReadOnlyList<OrganizationDto>> ListAsync(CurrentUser currentUser, CancellationToken ct = default);
    Task<OrganizationDto> SetAccessAsync(Guid id, UpdateOrganizationAccessRequest request, CurrentUser currentUser, CancellationToken ct = default);
}
