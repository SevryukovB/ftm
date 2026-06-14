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
    DateTime? Deadline,
    int? ReminderOffsetMinutes,
    long RewardAmountMinor,
    string RewardCurrency);

public sealed record TaskUpdatedEvent(
    Guid TaskId,
    string Title,
    Guid OrganizationId,
    Guid? AssigneeId,
    string? AssigneeName,
    Guid CreatedById,
    string CreatedByName,
    string Status,
    DateTime? Deadline,
    int? ReminderOffsetMinutes,
    long RewardAmountMinor,
    string RewardCurrency);

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
    DateTime? Deadline,
    int? ReminderOffsetMinutes,
    long RewardAmountMinor,
    string RewardCurrency);

public sealed record TaskReminderDueEvent(
    Guid TaskId,
    string Title,
    Guid OrganizationId,
    Guid? AssigneeId,
    string? AssigneeName,
    Guid CreatedById,
    string CreatedByName,
    DateTime Deadline,
    int ReminderOffsetMinutes);

public sealed record TaskCommentAddedEvent(
    Guid TaskId,
    string Title,
    Guid OrganizationId,
    Guid? AssigneeId,
    string? AssigneeName,
    Guid CreatedById,
    string CreatedByName,
    Guid AuthorId,
    string AuthorName,
    string CommentText);
