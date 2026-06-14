using FieldTaskManager.SchedulerService.Jobs;
using FieldTaskManager.SchedulerService.Models;
using Hangfire;
using MongoDB.Driver;

namespace FieldTaskManager.SchedulerService.Scheduling;

public sealed class TaskScheduleService(
    IBackgroundJobClient jobs,
    IMongoDatabase database,
    ILogger<TaskScheduleService> logger) : ITaskScheduleService
{
    private readonly IMongoCollection<TaskJobMapping> _mappings =
        database.GetCollection<TaskJobMapping>("task_job_mappings");

    public async Task ScheduleAsync(TaskSchedule schedule, CancellationToken ct)
    {
        await EnsureIndexesAsync(ct);
        await CancelAsync(schedule.TaskId, ct);

        if (schedule.Deadline is null || IsTerminal(schedule.Status))
        {
            logger.LogInformation("Task {TaskId} has no active deadline schedule.", schedule.TaskId);
            return;
        }

        var deadline = EnsureUtc(schedule.Deadline.Value);
        if (deadline <= DateTime.UtcNow)
        {
            logger.LogInformation("Task {TaskId} deadline is in the past; skipping schedule.", schedule.TaskId);
            return;
        }

        var deadlineJobId = jobs.Schedule<TaskDeadlineJob>(
            job => job.MarkNotCompletedAsync(schedule.TaskId, CancellationToken.None),
            new DateTimeOffset(deadline));

        string? reminderJobId = null;
        if (schedule.ReminderOffsetMinutes is int reminderOffset)
        {
            var reminderAt = deadline.AddMinutes(-reminderOffset);
            if (reminderAt > DateTime.UtcNow)
            {
                reminderJobId = jobs.Schedule<TaskReminderJob>(
                    job => job.PublishReminderAsync(schedule, CancellationToken.None),
                    new DateTimeOffset(reminderAt));
            }
        }

        var taskKey = schedule.TaskId.ToString();
        await _mappings.ReplaceOneAsync(
            x => x.TaskId == taskKey,
            new TaskJobMapping
            {
                TaskId = taskKey,
                DeadlineJobId = deadlineJobId,
                ReminderJobId = reminderJobId,
                Deadline = deadline,
                ReminderOffsetMinutes = schedule.ReminderOffsetMinutes,
                UpdatedAt = DateTime.UtcNow
            },
            new ReplaceOptions { IsUpsert = true },
            ct);

        logger.LogInformation(
            "Scheduled task {TaskId}: deadline job {DeadlineJobId}, reminder job {ReminderJobId}.",
            schedule.TaskId,
            deadlineJobId,
            reminderJobId ?? "-");
    }

    public async Task CancelAsync(Guid taskId, CancellationToken ct)
    {
        await EnsureIndexesAsync(ct);
        var taskKey = taskId.ToString();
        var existing = await _mappings.Find(x => x.TaskId == taskKey).FirstOrDefaultAsync(ct);
        if (existing is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(existing.DeadlineJobId))
        {
            BackgroundJob.Delete(existing.DeadlineJobId);
        }

        if (!string.IsNullOrWhiteSpace(existing.ReminderJobId))
        {
            BackgroundJob.Delete(existing.ReminderJobId);
        }

        await _mappings.DeleteOneAsync(x => x.TaskId == taskKey, ct);
        logger.LogInformation("Canceled scheduled jobs for task {TaskId}.", taskId);
    }

    private static bool IsTerminal(string status) =>
        status is "Done" or "Verified" or "NotCompleted";

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private Task EnsureIndexesAsync(CancellationToken ct) =>
        _mappings.Indexes.CreateOneAsync(
            new CreateIndexModel<TaskJobMapping>(
                Builders<TaskJobMapping>.IndexKeys.Ascending(x => x.UpdatedAt)),
            cancellationToken: ct);
}
