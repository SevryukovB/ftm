using FieldTaskManager.Domain.Enums;

namespace FieldTaskManager.Domain.Repositories;

public sealed record TaskFilter(
    FieldTaskStatus? Status = null,
    Guid? AssigneeId = null,
    string? Search = null);
