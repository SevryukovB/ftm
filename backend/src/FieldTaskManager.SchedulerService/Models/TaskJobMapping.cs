using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FieldTaskManager.SchedulerService.Models;

public sealed class TaskJobMapping
{
    [BsonId]
    public string TaskId { get; set; } = string.Empty;

    public string? DeadlineJobId { get; set; }
    public string? ReminderJobId { get; set; }
    public DateTime? Deadline { get; set; }
    public int? ReminderOffsetMinutes { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonExtraElements]
    public BsonDocument ExtraElements { get; set; } = [];
}
