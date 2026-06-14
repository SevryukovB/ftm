using FieldTaskManager.SchedulerService.Models;

namespace FieldTaskManager.SchedulerService.Scheduling;

public interface ITaskScheduleService
{
    Task ScheduleAsync(TaskSchedule schedule, CancellationToken ct);
    Task CancelAsync(Guid taskId, CancellationToken ct);
}
