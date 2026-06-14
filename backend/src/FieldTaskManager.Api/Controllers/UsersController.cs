using FieldTaskManager.Api.Extensions;
using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FieldTaskManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(CancellationToken ct) =>
        Ok(await userService.ListAsync(User.ToCurrentUser(), ct));

    [HttpGet("workers")]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> Workers(CancellationToken ct) =>
        Ok(await userService.ListWorkersAsync(User.ToCurrentUser(), ct));

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct) =>
        Ok(await userService.GetAsync(User.ToCurrentUser().Id, ct));

    [HttpPost]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var user = await userService.CreateAsync(request, User.ToCurrentUser(), ct);
        return CreatedAtAction(nameof(Me), new { id = user.Id }, user);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<ActionResult<UserDto>> Update(Guid id, UpdateUserRequest request, CancellationToken ct) =>
        Ok(await userService.UpdateAsync(id, request, User.ToCurrentUser(), ct));

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await userService.DeactivateAsync(id, User.ToCurrentUser(), ct);
        return NoContent();
    }
}
