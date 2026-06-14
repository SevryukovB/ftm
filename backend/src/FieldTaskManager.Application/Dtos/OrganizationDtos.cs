using System.ComponentModel.DataAnnotations;

namespace FieldTaskManager.Application.Dtos;

public sealed record OrganizationDto(
    Guid Id,
    string Name,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record UpdateOrganizationAccessRequest(bool IsActive);
