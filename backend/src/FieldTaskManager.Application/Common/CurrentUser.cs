using FieldTaskManager.Domain.Enums;

namespace FieldTaskManager.Application.Common;

public sealed record CurrentUser(Guid Id, UserRole Role)
{
    public bool IsAdmin => Role == UserRole.Admin;
}
