using FieldTaskManager.Domain.Entities;

namespace FieldTaskManager.Domain.Repositories;

public interface ITaskRepository : IRepository<TaskItem>
{
    Task<TaskItem?> GetDetailsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TaskItem>> SearchAsync(TaskFilter filter, CancellationToken ct = default);
}
