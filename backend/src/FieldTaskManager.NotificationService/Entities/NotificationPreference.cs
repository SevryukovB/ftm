namespace FieldTaskManager.NotificationService.Entities;

public class NotificationPreference
{
    public Guid UserId { get; set; }
    public bool Internal { get; set; } = true;
    public bool Email { get; set; }
    public bool Sms { get; set; }
    public bool Telegram { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
