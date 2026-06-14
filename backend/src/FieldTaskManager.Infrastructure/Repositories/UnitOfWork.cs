using FieldTaskManager.Domain.Entities;
using FieldTaskManager.Domain.Repositories;
using FieldTaskManager.Infrastructure.Persistence;

namespace FieldTaskManager.Infrastructure.Repositories;

public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private ITaskRepository? _tasks;
    private IUserRepository? _users;
    private IRepository<TaskComment>? _comments;

    public ITaskRepository Tasks => _tasks ??= new TaskRepository(context);
    public IUserRepository Users => _users ??= new UserRepository(context);
    public IRepository<TaskComment> Comments => _comments ??= new Repository<TaskComment>(context);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
