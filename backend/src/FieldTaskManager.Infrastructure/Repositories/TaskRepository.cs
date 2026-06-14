using FieldTaskManager.Domain.Entities;
using FieldTaskManager.Domain.Repositories;
using FieldTaskManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FieldTaskManager.Infrastructure.Repositories;

public sealed class TaskRepository(AppDbContext context) : Repository<TaskItem>(context), ITaskRepository
{
    public async Task<TaskItem?> GetDetailsAsync(Guid id, CancellationToken ct = default) =>
        await Set
            .Include(t => t.Assignee)
            .Include(t => t.CreatedBy)
            .Include(t => t.Comments.OrderBy(c => c.CreatedAt))
                .ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<TaskItem>> SearchAsync(Guid organizationId, TaskFilter filter, CancellationToken ct = default)
    {
        var query = Set
            .AsNoTracking()
            .Include(t => t.Assignee)
            .Include(t => t.CreatedBy)
            .Where(t => t.OrganizationId == organizationId)
            .AsQueryable();

        if (filter.Status is not null)
        {
            query = query.Where(t => t.Status == filter.Status);
        }

        if (filter.AssigneeId is not null)
        {
            query = query.Where(t => t.AssigneeId == filter.AssigneeId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = $"%{filter.Search.Trim()}%";
            query = query.Where(t =>
                EF.Functions.ILike(t.Title, pattern) ||
                EF.Functions.ILike(t.Description, pattern));
        }

        if (filter.UpdatedFrom is not null)
        {
            query = query.Where(t => t.UpdatedAt >= filter.UpdatedFrom.Value);
        }

        if (filter.UpdatedTo is not null)
        {
            query = query.Where(t => t.UpdatedAt < filter.UpdatedTo.Value);
        }

        return await query.OrderByDescending(t => t.UpdatedAt).ToListAsync(ct);
    }
}
