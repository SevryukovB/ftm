using System.Security.Claims;

namespace FieldTaskManager.EarningsService.Extensions;

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

    public static Guid GetOrganizationId(this ClaimsPrincipal principal)
    {
        var idValue = principal.FindFirstValue("organization_id");
        if (!Guid.TryParse(idValue, out var id))
        {
            throw new UnauthorizedAccessException("Current user is not linked to an organization.");
        }

        return id;
    }

    public static bool IsOrgAdmin(this ClaimsPrincipal principal) =>
        principal.IsInRole("OrgAdmin");
}
