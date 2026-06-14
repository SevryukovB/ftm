namespace FieldTaskManager.Application.Messaging;

public sealed record OutboxEventEnvelope(
    Guid EventId,
    string Type,
    Guid AggregateId,
    DateTime OccurredAt,
    object Payload);

public sealed record TaskCreatedEvent(
    Guid TaskId,
    string Title,
    Guid OrganizationId,
    Guid? AssigneeId,
    string? AssigneeName,
    Guid CreatedById,
    string CreatedByName,
    DateTime? Deadline);

public sealed record TaskStatusChangedEvent(
    Guid TaskId,
    string Title,
    Guid OrganizationId,
    string Status,
    Guid? AssigneeId,
    string? AssigneeName,
    Guid CreatedById,
    string CreatedByName,
    Guid ChangedById,
    DateTime? Deadline);
