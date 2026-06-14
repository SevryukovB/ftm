namespace FieldTaskManager.NotificationService.Dtos;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Message,
    string PayloadJson,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);

public sealed record NotificationPreferenceDto(
    bool Internal,
    bool Email,
    bool Sms,
    string? PhoneNumber,
    bool Telegram,
    string? TelegramUsername);

public sealed record UpdateNotificationPreferenceRequest(
    bool Email,
    bool Sms,
    string? PhoneNumber,
    bool Telegram,
    string? TelegramUsername);

public sealed record UnreadCountDto(int Count);
