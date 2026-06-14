using FieldTaskManager.Domain.Entities;
using FieldTaskManager.Domain.Enums;
using FieldTaskManager.Domain.Repositories;
using FieldTaskManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FieldTaskManager.Infrastructure.Repositories;

public sealed class UserRepository(AppDbContext context) : Repository<User>(context), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await Set.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<User?> GetByEmailWithOrganizationAsync(string email, CancellationToken ct = default) =>
        await Set.Include(u => u.Organization).FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        await Set.AnyAsync(u => u.Email == email, ct);

    public async Task<IReadOnlyList<User>> ListByOrganizationAsync(Guid organizationId, CancellationToken ct = default) =>
        await Set.AsNoTracking()
            .Where(u => u.OrganizationId == organizationId)
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<User>> ListByRoleAsync(Guid organizationId, UserRole role, CancellationToken ct = default) =>
        await Set.AsNoTracking()
            .Where(u => u.OrganizationId == organizationId && u.Role == role)
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);
}
