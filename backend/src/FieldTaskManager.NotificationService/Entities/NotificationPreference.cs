namespace FieldTaskManager.NotificationService.Entities;

public class NotificationPreference
{
    public Guid UserId { get; set; }
    public bool Internal { get; set; } = true;
    public bool Email { get; set; }
    public bool Sms { get; set; }
    public string? PhoneNumber { get; set; }
    public bool Telegram { get; set; }
    public string? TelegramUsername { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
