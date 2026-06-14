using System.Text.Json;
using Confluent.Kafka;
using FieldTaskManager.NotificationService.Entities;
using FieldTaskManager.NotificationService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FieldTaskManager.NotificationService.Messaging;

public sealed class TaskEventsConsumerHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> options,
    ILogger<TaskEventsConsumerHostedService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var config = new ConsumerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            GroupId = options.Value.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = true
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            try
            {
                consumer.Subscribe(options.Value.TaskEventsTopic);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var result = consumer.Consume(stoppingToken);
                    await ProcessAsync(result.Message.Value, stoppingToken);
                    consumer.Commit(result);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Task event consumer failed. Reconnecting...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            finally
            {
                consumer.Close();
            }
        }
    }

    private async Task ProcessAsync(string message, CancellationToken ct)
    {
        var envelope = JsonSerializer.Deserialize<TaskEventEnvelope>(message, JsonOptions);
        if (envelope is null)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        if (await context.ProcessedEvents.AnyAsync(e => e.EventId == envelope.EventId, ct))
        {
            return;
        }

        var notifications = BuildNotifications(envelope);
        foreach (var notification in notifications)
        {
            var preferences = await GetOrCreatePreferencesAsync(context, notification.UserId, ct);
            AddMockDeliveries(notification, preferences);
            context.Notifications.Add(notification);
        }

        context.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = envelope.EventId,
            Type = envelope.Type
        });

        await context.SaveChangesAsync(ct);
    }

    private static IReadOnlyList<Notification> BuildNotifications(TaskEventEnvelope envelope)
    {
        var payload = envelope.Payload;
        var taskId = payload.GetProperty("taskId").GetGuid();
        var title = payload.GetProperty("title").GetString() ?? "Task";

        return envelope.Type switch
        {
            "TaskCreated" => ToList(BuildForOptionalRecipient(
                payload,
                "assigneeId",
                "Task assigned",
                $"New task assigned: {title}",
                "TaskCreated",
                taskId,
                envelope.Payload.GetRawText())),
            "TaskDone" => [BuildForRequiredRecipient(
                payload,
                "createdById",
                "Task completed",
                $"Task is ready for verification: {title}",
                "TaskDone",
                taskId,
                envelope.Payload.GetRawText())],
            "TaskVerified" => ToList(BuildForOptionalRecipient(
                payload,
                "assigneeId",
                "Task verified",
                $"Task was verified and closed: {title}",
                "TaskVerified",
                taskId,
                envelope.Payload.GetRawText())),
            "TaskNotCompleted" => BuildParticipantNotifications(
                payload,
                "Task not completed",
                $"Task missed its deadline: {title}",
                "TaskNotCompleted",
                taskId,
                envelope.Payload.GetRawText()),
            "TaskReminderDue" => BuildParticipantNotifications(
                payload,
                "Task reminder",
                $"Task is due in {FormatReminderOffset(payload)}: {title}",
                "TaskReminderDue",
                taskId,
                envelope.Payload.GetRawText()),
            "TaskCommentAdded" => BuildCommentNotifications(payload, taskId, title, envelope.Payload.GetRawText()),
            _ => []
        };
    }

    private static IReadOnlyList<Notification> BuildParticipantNotifications(
        JsonElement payload,
        string title,
        string message,
        string type,
        Guid taskId,
        string payloadJson)
    {
        var recipientIds = GetParticipantIds(payload);
        return recipientIds
            .Select(userId => CreateNotification(userId, title, message, type, taskId, payloadJson))
            .ToList();
    }

    private static IReadOnlyList<Notification> BuildCommentNotifications(
        JsonElement payload,
        Guid taskId,
        string taskTitle,
        string payloadJson)
    {
        var authorId = payload.GetProperty("authorId").GetGuid();
        var authorName = payload.GetProperty("authorName").GetString() ?? "Someone";
        var commentText = payload.GetProperty("commentText").GetString() ?? string.Empty;
        var shortComment = commentText.Length <= 120 ? commentText : $"{commentText[..120]}...";

        var recipientIds = GetParticipantIds(payload);
        recipientIds.Remove(authorId);

        return recipientIds
            .Select(userId => CreateNotification(
                userId,
                "New comment",
                $"{authorName} commented on {taskTitle}: {shortComment}",
                "TaskCommentAdded",
                taskId,
                payloadJson))
            .ToList();
    }

    private static HashSet<Guid> GetParticipantIds(JsonElement payload)
    {
        var recipientIds = new HashSet<Guid>();
        if (payload.TryGetProperty("assigneeId", out var assigneeId) && assigneeId.ValueKind != JsonValueKind.Null)
        {
            recipientIds.Add(assigneeId.GetGuid());
        }

        recipientIds.Add(payload.GetProperty("createdById").GetGuid());
        return recipientIds;
    }

    private static string FormatReminderOffset(JsonElement payload)
    {
        if (!payload.TryGetProperty("reminderOffsetMinutes", out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return "the configured reminder window";
        }

        var minutes = value.GetInt32();
        return minutes switch
        {
            60 => "1 hour",
            240 => "4 hours",
            1440 => "1 day",
            _ when minutes % 1440 == 0 => $"{minutes / 1440} days",
            _ when minutes % 60 == 0 => $"{minutes / 60} hours",
            _ => $"{minutes} minutes"
        };
    }

    private static Notification? BuildForOptionalRecipient(
        JsonElement payload,
        string property,
        string title,
        string message,
        string type,
        Guid taskId,
        string payloadJson)
    {
        if (!payload.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return CreateNotification(value.GetGuid(), title, message, type, taskId, payloadJson);
    }

    private static Notification BuildForRequiredRecipient(
        JsonElement payload,
        string property,
        string title,
        string message,
        string type,
        Guid taskId,
        string payloadJson) =>
        CreateNotification(payload.GetProperty(property).GetGuid(), title, message, type, taskId, payloadJson);

    private static Notification CreateNotification(
        Guid userId,
        string title,
        string message,
        string type,
        Guid taskId,
        string payloadJson) =>
        new()
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            PayloadJson = payloadJson
        };

    private static IReadOnlyList<Notification> ToList(Notification? notification) =>
        notification is null ? [] : [notification];

    private static async Task<NotificationPreference> GetOrCreatePreferencesAsync(
        NotificationDbContext context,
        Guid userId,
        CancellationToken ct)
    {
        var preferences = await context.Preferences.FindAsync([userId], ct);
        if (preferences is not null)
        {
            return preferences;
        }

        preferences = new NotificationPreference { UserId = userId, Internal = true };
        context.Preferences.Add(preferences);
        return preferences;
    }

    private void AddMockDeliveries(Notification notification, NotificationPreference preferences)
    {
        notification.DeliveryAttempts.Add(CreateAttempt("Internal", "Stored in app notification inbox."));

        if (preferences.Email)
        {
            notification.DeliveryAttempts.Add(CreateAttempt("Email", "Mock email delivery."));
        }

        if (preferences.Sms)
        {
            notification.DeliveryAttempts.Add(CreateAttempt("Sms", $"Mock SMS delivery to {preferences.PhoneNumber ?? "not configured"}."));
        }

        if (preferences.Telegram)
        {
            notification.DeliveryAttempts.Add(CreateAttempt("Telegram", $"Mock Telegram delivery to @{preferences.TelegramUsername ?? "not configured"}."));
        }

        foreach (var attempt in notification.DeliveryAttempts)
        {
            logger.LogInformation(
                "Mock notification delivery via {Channel} to user {UserId}: {Title}",
                attempt.Channel,
                notification.UserId,
                notification.Title);
        }
    }

    private static DeliveryAttempt CreateAttempt(string channel, string details) =>
        new()
        {
            Channel = channel,
            Details = details
        };
}
