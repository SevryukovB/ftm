using FieldTaskManager.Domain.Enums;

namespace FieldTaskManager.Application.Common;

public sealed record CurrentUser(Guid Id, UserRole Role, Guid? OrganizationId)
{
    public bool IsSuperAdmin => Role == UserRole.SuperAdmin;
    public bool IsOrgAdmin => Role == UserRole.OrgAdmin;
    public bool IsWorker => Role == UserRole.Worker;
}
