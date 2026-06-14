using FieldTaskManager.Api.Extensions;
using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FieldTaskManager.Api.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/organizations")]
public sealed class OrganizationsController(IOrganizationService organizationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrganizationDto>>> List(CancellationToken ct) =>
        (await organizationService.ListAsync(User.ToCurrentUser(), ct)).ToActionResult(this);

    [HttpPut("{id:guid}/access")]
    public async Task<ActionResult<OrganizationDto>> SetAccess(
        Guid id,
        UpdateOrganizationAccessRequest request,
        CancellationToken ct) =>
        (await organizationService.SetAccessAsync(id, request, User.ToCurrentUser(), ct)).ToActionResult(this);
}
