using FieldTaskManager.Api.Extensions;
using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FieldTaskManager.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/internal/tasks")]
public sealed class InternalTasksController(ITaskService taskService, IConfiguration configuration) : ControllerBase
{
    private const string InternalApiKeyHeader = "X-Internal-Api-Key";

    [HttpPost("{id:guid}/mark-not-completed")]
    public async Task<ActionResult<TaskDto>> MarkNotCompleted(Guid id, CancellationToken ct)
    {
        var expectedKey = configuration["InternalApi:Key"];
        if (string.IsNullOrWhiteSpace(expectedKey) ||
            !Request.Headers.TryGetValue(InternalApiKeyHeader, out var providedKey) ||
            !StringComparer.Ordinal.Equals(expectedKey, providedKey.ToString()))
        {
            return Unauthorized(new { message = "Invalid internal API key." });
        }

        return (await taskService.MarkNotCompletedAsync(id, ct)).ToActionResult(this);
    }
}
