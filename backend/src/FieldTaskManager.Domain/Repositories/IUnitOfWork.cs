using FieldTaskManager.Domain.Entities;

namespace FieldTaskManager.Domain.Repositories;

public interface IUnitOfWork
{
    ITaskRepository Tasks { get; }
    IUserRepository Users { get; }
    IRepository<Organization> Organizations { get; }
    IRepository<TaskComment> Comments { get; }
    IOutboxRepository Outbox { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
