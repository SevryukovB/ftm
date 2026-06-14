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
        (await userService.ListAsync(User.ToCurrentUser(), ct)).ToActionResult(this);

    [HttpGet("workers")]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> Workers(CancellationToken ct) =>
        (await userService.ListWorkersAsync(User.ToCurrentUser(), ct)).ToActionResult(this);

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct) =>
        (await userService.GetAsync(User.ToCurrentUser().Id, ct)).ToActionResult(this);

    [HttpPost]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var result = await userService.CreateAsync(request, User.ToCurrentUser(), ct);
        if (result.IsFailure)
        {
            return result.ToFailureActionResult<UserDto>(this);
        }

        var user = result.Value;
        return CreatedAtAction(nameof(Me), new { id = user.Id }, user);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<ActionResult<UserDto>> Update(Guid id, UpdateUserRequest request, CancellationToken ct) =>
        (await userService.UpdateAsync(id, request, User.ToCurrentUser(), ct)).ToActionResult(this);

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        return (await userService.DeactivateAsync(id, User.ToCurrentUser(), ct)).ToActionResult(this);
    }
}
