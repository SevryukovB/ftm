using FieldTaskManager.Domain.Entities;

namespace FieldTaskManager.Domain.Repositories;

public interface IUnitOfWork
{
    ITaskRepository Tasks { get; }
    IUserRepository Users { get; }
    IRepository<TaskComment> Comments { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
