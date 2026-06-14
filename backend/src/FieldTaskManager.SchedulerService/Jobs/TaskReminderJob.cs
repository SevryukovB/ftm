using System.Text.Json;
using Confluent.Kafka;
using FieldTaskManager.SchedulerService.Messaging;
using FieldTaskManager.SchedulerService.Models;
using Microsoft.Extensions.Options;

namespace FieldTaskManager.SchedulerService.Jobs;

public sealed class TaskReminderJob(
    IOptions<KafkaOptions> options,
    ILogger<TaskReminderJob> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task PublishReminderAsync(TaskSchedule schedule, CancellationToken ct = default)
    {
        if (schedule.Deadline is null || schedule.ReminderOffsetMinutes is null)
        {
            return;
        }

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            AllowAutoCreateTopics = true
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();
        var occurredAt = DateTime.UtcNow;
        var payload = new
        {
            schedule.TaskId,
            schedule.Title,
            schedule.OrganizationId,
            schedule.AssigneeId,
            schedule.AssigneeName,
            schedule.CreatedById,
            schedule.CreatedByName,
            Deadline = schedule.Deadline,
            ReminderOffsetMinutes = schedule.ReminderOffsetMinutes.Value
        };
        var envelope = new
        {
            EventId = Guid.NewGuid(),
            Type = "TaskReminderDue",
            AggregateId = schedule.TaskId,
            OccurredAt = occurredAt,
            Payload = payload
        };

        await producer.ProduceAsync(
            options.Value.TaskEventsTopic,
            new Message<string, string>
            {
                Key = schedule.TaskId.ToString(),
                Value = JsonSerializer.Serialize(envelope, JsonOptions)
            },
            ct);

        logger.LogInformation("Published task reminder for task {TaskId}.", schedule.TaskId);
    }
}
