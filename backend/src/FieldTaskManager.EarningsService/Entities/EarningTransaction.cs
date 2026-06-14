namespace FieldTaskManager.EarningsService.Entities;

public class EarningTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? SourceEventId { get; set; }
    public Guid? TaskId { get; set; }
    public string? TaskTitle { get; set; }
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public long AmountMinor { get; set; }
    public string Currency { get; set; } = "UAH";
    public string Type { get; set; } = "TaskReward";
    public string Status { get; set; } = "Confirmed";
    public string? Description { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
