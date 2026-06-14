namespace FieldTaskManager.SchedulerService.Models;

public sealed record TaskSchedule(
    Guid TaskId,
    string Title,
    Guid OrganizationId,
    Guid? AssigneeId,
    string? AssigneeName,
    Guid CreatedById,
    string CreatedByName,
    string Status,
    DateTime? Deadline,
    int? ReminderOffsetMinutes);
