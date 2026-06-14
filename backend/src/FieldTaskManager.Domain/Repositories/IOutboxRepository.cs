using FieldTaskManager.Domain.Entities;

namespace FieldTaskManager.Domain.Repositories;

public interface IOutboxRepository
{
    void Add(OutboxMessage message);
}
