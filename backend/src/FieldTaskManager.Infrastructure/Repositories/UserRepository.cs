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

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        await Set.AnyAsync(u => u.Email == email, ct);

    public async Task<IReadOnlyList<User>> ListByRoleAsync(UserRole role, CancellationToken ct = default) =>
        await Set.AsNoTracking()
            .Where(u => u.Role == role)
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);
}
