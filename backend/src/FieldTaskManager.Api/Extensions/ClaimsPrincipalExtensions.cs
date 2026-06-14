using System.Security.Claims;
using FieldTaskManager.Application.Common;
using FieldTaskManager.Application.Exceptions;
using FieldTaskManager.Domain.Enums;

namespace FieldTaskManager.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static CurrentUser ToCurrentUser(this ClaimsPrincipal principal)
    {
        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleValue = principal.FindFirstValue(ClaimTypes.Role);
        var organizationIdValue = principal.FindFirstValue("organization_id");

        if (!Guid.TryParse(idValue, out var id) || !Enum.TryParse<UserRole>(roleValue, out var role))
        {
            throw new UnauthorizedException("Invalid authentication token.");
        }

        Guid? organizationId = null;
        if (!string.IsNullOrWhiteSpace(organizationIdValue))
        {
            if (!Guid.TryParse(organizationIdValue, out var parsedOrganizationId))
            {
                throw new UnauthorizedException("Invalid authentication token.");
            }

            organizationId = parsedOrganizationId;
        }

        return new CurrentUser(id, role, organizationId);
    }
}
