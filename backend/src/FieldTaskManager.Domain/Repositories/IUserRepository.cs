using FieldTaskManager.Domain.Entities;
using FieldTaskManager.Domain.Enums;

namespace FieldTaskManager.Domain.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<User>> ListByRoleAsync(UserRole role, CancellationToken ct = default);
}
