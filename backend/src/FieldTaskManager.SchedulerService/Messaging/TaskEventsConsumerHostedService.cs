using System.Text.Json;
using Confluent.Kafka;
using FieldTaskManager.SchedulerService.Models;
using FieldTaskManager.SchedulerService.Scheduling;
using Microsoft.Extensions.Options;

namespace FieldTaskManager.SchedulerService.Messaging;

public sealed class TaskEventsConsumerHostedService(
    ITaskScheduleService scheduler,
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
                logger.LogError(ex, "Scheduler task event consumer failed. Reconnecting...");
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

        switch (envelope.Type)
        {
            case "TaskCreated":
            case "TaskUpdated":
                var schedule = ToSchedule(envelope);
                if (schedule is not null)
                {
                    await scheduler.ScheduleAsync(schedule, ct);
                }
                break;

            case "TaskDone":
            case "TaskVerified":
            case "TaskNotCompleted":
                await scheduler.CancelAsync(envelope.AggregateId, ct);
                break;
        }
    }

    private static TaskSchedule? ToSchedule(TaskEventEnvelope envelope)
    {
        var payload = envelope.Payload;
        if (!payload.TryGetProperty("taskId", out var taskId))
        {
            return null;
        }

        return new TaskSchedule(
            taskId.GetGuid(),
            payload.GetProperty("title").GetString() ?? "Task",
            payload.GetProperty("organizationId").GetGuid(),
            GetNullableGuid(payload, "assigneeId"),
            GetNullableString(payload, "assigneeName"),
            payload.GetProperty("createdById").GetGuid(),
            payload.GetProperty("createdByName").GetString() ?? "Administrator",
            GetNullableString(payload, "status") ?? "Created",
            GetNullableDateTime(payload, "deadline"),
            GetNullableInt(payload, "reminderOffsetMinutes"));
    }

    private static Guid? GetNullableGuid(JsonElement payload, string property) =>
        payload.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.GetGuid()
            : null;

    private static string? GetNullableString(JsonElement payload, string property) =>
        payload.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.GetString()
            : null;

    private static DateTime? GetNullableDateTime(JsonElement payload, string property) =>
        payload.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.GetDateTime()
            : null;

    private static int? GetNullableInt(JsonElement payload, string property) =>
        payload.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.GetInt32()
            : null;
}
