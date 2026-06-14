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
        Ok(await taskService.SearchAsync(new TaskFilter(status, assigneeId, search), User.ToCurrentUser(), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await taskService.GetAsync(id, User.ToCurrentUser(), ct));

    [HttpPost]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<ActionResult<TaskDto>> Create(CreateTaskRequest request, CancellationToken ct)
    {
        var task = await taskService.CreateAsync(request, User.ToCurrentUser(), ct);
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<ActionResult<TaskDto>> Update(Guid id, UpdateTaskRequest request, CancellationToken ct) =>
        Ok(await taskService.UpdateAsync(id, request, User.ToCurrentUser(), ct));

    [HttpPut("{id:guid}/location")]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<ActionResult<TaskDto>> UpdateLocation(Guid id, UpdateLocationRequest request, CancellationToken ct) =>
        Ok(await taskService.UpdateLocationAsync(id, request, User.ToCurrentUser(), ct));

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<TaskDto>> ChangeStatus(Guid id, ChangeStatusRequest request, CancellationToken ct) =>
        Ok(await taskService.ChangeStatusAsync(id, request, User.ToCurrentUser(), ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await taskService.DeleteAsync(id, User.ToCurrentUser(), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<CommentDto>> AddComment(Guid id, AddCommentRequest request, CancellationToken ct) =>
        Ok(await taskService.AddCommentAsync(id, request, User.ToCurrentUser(), ct));
}
