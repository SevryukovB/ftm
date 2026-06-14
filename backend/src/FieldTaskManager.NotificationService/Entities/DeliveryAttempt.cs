namespace FieldTaskManager.NotificationService.Entities;

public class DeliveryAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = "Mocked";
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
