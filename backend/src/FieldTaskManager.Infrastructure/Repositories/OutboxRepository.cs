using FieldTaskManager.Domain.Entities;
using FieldTaskManager.Domain.Repositories;
using FieldTaskManager.Infrastructure.Persistence;

namespace FieldTaskManager.Infrastructure.Repositories;

public sealed class OutboxRepository(AppDbContext context) : IOutboxRepository
{
    public void Add(OutboxMessage message) => context.OutboxMessages.Add(message);
}
