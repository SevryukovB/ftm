using FieldTaskManager.Api.Extensions;
using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Application.Interfaces;
using FieldTaskManager.Domain.Enums;
using FieldTaskManager.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FieldTaskManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public sealed class TasksController(ITaskService taskService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> Search(
        [FromQuery] FieldTaskStatus? status,
        [FromQuery] Guid? assigneeId,
        [FromQuery] string? search,
        CancellationToken ct) =>
        (await taskService.SearchAsync(new TaskFilter(status, assigneeId, search), User.ToCurrentUser(), ct)).ToActionResult(this);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDto>> Get(Guid id, CancellationToken ct) =>
        (await taskService.GetAsync(id, User.ToCurrentUser(), ct)).ToActionResult(this);

    [HttpPost]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<ActionResult<TaskDto>> Create(CreateTaskRequest request, CancellationToken ct)
    {
        var result = await taskService.CreateAsync(request, User.ToCurrentUser(), ct);
        if (result.IsFailure)
        {
            return result.ToFailureActionResult<TaskDto>(this);
        }

        var task = result.Value;
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<ActionResult<TaskDto>> Update(Guid id, UpdateTaskRequest request, CancellationToken ct) =>
        (await taskService.UpdateAsync(id, request, User.ToCurrentUser(), ct)).ToActionResult(this);

    [HttpPut("{id:guid}/location")]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<ActionResult<TaskDto>> UpdateLocation(Guid id, UpdateLocationRequest request, CancellationToken ct) =>
        (await taskService.UpdateLocationAsync(id, request, User.ToCurrentUser(), ct)).ToActionResult(this);

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<TaskDto>> ChangeStatus(Guid id, ChangeStatusRequest request, CancellationToken ct) =>
        (await taskService.ChangeStatusAsync(id, request, User.ToCurrentUser(), ct)).ToActionResult(this);

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        return (await taskService.DeleteAsync(id, User.ToCurrentUser(), ct)).ToActionResult(this);
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<CommentDto>> AddComment(Guid id, AddCommentRequest request, CancellationToken ct) =>
        (await taskService.AddCommentAsync(id, request, User.ToCurrentUser(), ct)).ToActionResult(this);
}
