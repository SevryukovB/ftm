using System.Security.Claims;

namespace FieldTaskManager.NotificationService.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idValue, out var id))
        {
            throw new UnauthorizedAccessException("Invalid authentication token.");
        }

        return id;
    }
}
