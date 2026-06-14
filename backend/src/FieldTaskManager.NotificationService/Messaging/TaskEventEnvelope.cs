using System.Text.Json;

namespace FieldTaskManager.NotificationService.Messaging;

public sealed record TaskEventEnvelope(
    Guid EventId,
    string Type,
    Guid AggregateId,
    DateTime OccurredAt,
    JsonElement Payload);
