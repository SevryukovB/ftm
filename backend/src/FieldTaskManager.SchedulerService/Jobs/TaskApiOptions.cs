namespace FieldTaskManager.SchedulerService.Jobs;

public sealed class TaskApiOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5000";
    public string InternalApiKey { get; set; } = "ftm-internal-dev-key";
}
