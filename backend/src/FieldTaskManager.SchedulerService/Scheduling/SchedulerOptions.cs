namespace FieldTaskManager.SchedulerService.Scheduling;

public sealed class SchedulerOptions
{
    public string MongoConnectionString { get; set; } = "mongodb://localhost:27017";
    public string MongoDatabase { get; set; } = "ftm_scheduler";
}
