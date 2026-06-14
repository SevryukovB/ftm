using FieldTaskManager.NotificationService.Dtos;
using FieldTaskManager.NotificationService.Entities;
using FieldTaskManager.NotificationService.Extensions;
using FieldTaskManager.NotificationService.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FieldTaskManager.NotificationService.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(NotificationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> List(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);

        var query = context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(n => new NotificationDto(n.Id, n.Type, n.Title, n.Message, n.PayloadJson, n.IsRead, n.ReadAt, n.CreatedAt))
            .ToListAsync(ct);

        return notifications;
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadCountDto>> UnreadCount(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var count = await context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);
        return new UnreadCountDto(count);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var notification = await context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct);
        if (notification is null)
        {
            return NotFound();
        }

        notification.IsRead = true;
        notification.ReadAt ??= DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        var userId = User.GetUserId();
        await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
        return NoContent();
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<NotificationPreferenceDto>> GetPreferences(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var preferences = await GetOrCreatePreferencesAsync(userId, ct);
        return ToDto(preferences);
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<NotificationPreferenceDto>> UpdatePreferences(
        UpdateNotificationPreferenceRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        var preferences = await GetOrCreatePreferencesAsync(userId, ct);
        preferences.Internal = true;
        preferences.Email = request.Email;
        preferences.Sms = request.Sms;
        preferences.PhoneNumber = request.Sms ? NormalizeOptional(request.PhoneNumber) : null;
        preferences.Telegram = request.Telegram;
        preferences.TelegramUsername = request.Telegram ? NormalizeTelegram(request.TelegramUsername) : null;
        preferences.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        return ToDto(preferences);
    }

    private async Task<NotificationPreference> GetOrCreatePreferencesAsync(Guid userId, CancellationToken ct)
    {
        var preferences = await context.Preferences.FindAsync([userId], ct);
        if (preferences is not null)
        {
            return preferences;
        }

        preferences = new NotificationPreference { UserId = userId, Internal = true };
        context.Preferences.Add(preferences);
        await context.SaveChangesAsync(ct);
        return preferences;
    }

    private static NotificationPreferenceDto ToDto(NotificationPreference preferences) =>
        new(
            true,
            preferences.Email,
            preferences.Sms,
            preferences.PhoneNumber,
            preferences.Telegram,
            preferences.TelegramUsername);

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? NormalizeTelegram(string? value)
    {
        var normalized = NormalizeOptional(value);
        return normalized?.TrimStart('@');
    }
}
