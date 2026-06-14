using FieldTaskManager.Domain.Entities;
using FieldTaskManager.Domain.Enums;

namespace FieldTaskManager.Domain.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByEmailWithOrganizationAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<User>> ListByOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<User>> ListByRoleAsync(Guid organizationId, UserRole role, CancellationToken ct = default);
}
