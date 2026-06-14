using FieldTaskManager.Application.Common;
using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Application.Interfaces;
using FieldTaskManager.Application.Mapping;
using FieldTaskManager.Domain.Entities;
using FieldTaskManager.Domain.Enums;
using FieldTaskManager.Domain.Repositories;

namespace FieldTaskManager.Application.Services;

public sealed class TaskService(IUnitOfWork unitOfWork) : ITaskService
{
    // Allowed status transitions per role.
    // Worker (assignee only): Created -> InProgress -> Done.
    // OrgAdmin: Done -> Verified (approve) or Done -> InProgress (return to work).
    private static readonly Dictionary<UserRole, (FieldTaskStatus From, FieldTaskStatus To)[]> Transitions = new()
    {
        [UserRole.Worker] =
        [
            (FieldTaskStatus.Created, FieldTaskStatus.InProgress),
            (FieldTaskStatus.InProgress, FieldTaskStatus.Done)
        ],
        [UserRole.OrgAdmin] =
        [
            (FieldTaskStatus.Done, FieldTaskStatus.Verified),
            (FieldTaskStatus.Done, FieldTaskStatus.InProgress)
        ]
    };

    public async Task<Result<IReadOnlyList<TaskDto>>> SearchAsync(TaskFilter filter, CurrentUser user, CancellationToken ct = default)
    {
        var organizationResult = RequireOrganization(user);
        if (organizationResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<TaskDto>>(organizationResult.Error);
        }

        var organizationId = organizationResult.Value;
        var effectiveFilter = user.IsOrgAdmin ? filter : filter with { AssigneeId = user.Id };
        var tasks = await unitOfWork.Tasks.SearchAsync(organizationId, effectiveFilter, ct);
        return Result.Success<IReadOnlyList<TaskDto>>(tasks.Select(t => t.ToDto()).ToList());
    }

    public async Task<Result<TaskDto>> GetAsync(Guid id, CurrentUser user, CancellationToken ct = default)
    {
        var taskResult = await GetTaskAsync(id, ct);
        if (taskResult.IsFailure)
        {
            return Result.Failure<TaskDto>(taskResult.Error);
        }

        var task = taskResult.Value;
        var canView = EnsureCanView(task, user);
        if (canView.IsFailure)
        {
            return Result.Failure<TaskDto>(canView.Error);
        }

        return Result.Success(task.ToDto(includeComments: true));
    }

    public async Task<Result<TaskDto>> CreateAsync(CreateTaskRequest request, CurrentUser user, CancellationToken ct = default)
    {
        var canManage = EnsureOrgAdmin(user);
        if (canManage.IsFailure)
        {
            return Result.Failure<TaskDto>(canManage.Error);
        }

        var organizationResult = RequireOrganization(user);
        if (organizationResult.IsFailure)
        {
            return Result.Failure<TaskDto>(organizationResult.Error);
        }

        var organizationId = organizationResult.Value;
        var assigneeResult = await EnsureAssigneeIsWorkerAsync(request.AssigneeId, organizationId, ct);
        if (assigneeResult.IsFailure)
        {
            return Result.Failure<TaskDto>(assigneeResult.Error);
        }

        var task = new TaskItem
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            OrganizationId = organizationId,
            AssigneeId = request.AssigneeId,
            Deadline = NormalizeUtc(request.Deadline),
            CreatedById = user.Id
        };

        unitOfWork.Tasks.Add(task);
        await unitOfWork.SaveChangesAsync(ct);

        var createdTask = await GetTaskAsync(task.Id, ct);
        return createdTask.IsSuccess
            ? Result.Success(createdTask.Value.ToDto())
            : Result.Failure<TaskDto>(createdTask.Error);
    }

    public async Task<Result<TaskDto>> UpdateAsync(Guid id, UpdateTaskRequest request, CurrentUser user, CancellationToken ct = default)
    {
        var canManage = EnsureOrgAdmin(user);
        if (canManage.IsFailure)
        {
            return Result.Failure<TaskDto>(canManage.Error);
        }

        var organizationResult = RequireOrganization(user);
        if (organizationResult.IsFailure)
        {
            return Result.Failure<TaskDto>(organizationResult.Error);
        }

        var organizationId = organizationResult.Value;
        var assigneeResult = await EnsureAssigneeIsWorkerAsync(request.AssigneeId, organizationId, ct);
        if (assigneeResult.IsFailure)
        {
            return Result.Failure<TaskDto>(assigneeResult.Error);
        }

        var taskResult = await GetTaskAsync(id, ct);
        if (taskResult.IsFailure)
        {
            return Result.Failure<TaskDto>(taskResult.Error);
        }

        var task = taskResult.Value;
        var sameOrganization = EnsureSameOrganization(task, user);
        if (sameOrganization.IsFailure)
        {
            return Result.Failure<TaskDto>(sameOrganization.Error);
        }

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim() ?? string.Empty;
        task.Latitude = request.Latitude;
        task.Longitude = request.Longitude;
        task.AssigneeId = request.AssigneeId;
        task.Deadline = NormalizeUtc(request.Deadline);
        task.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(ct);
        var updatedTask = await GetTaskAsync(id, ct);
        return updatedTask.IsSuccess
            ? Result.Success(updatedTask.Value.ToDto())
            : Result.Failure<TaskDto>(updatedTask.Error);
    }

    public async Task<Result<TaskDto>> UpdateLocationAsync(Guid id, UpdateLocationRequest request, CurrentUser user, CancellationToken ct = default)
    {
        var canManage = EnsureOrgAdmin(user);
        if (canManage.IsFailure)
        {
            return Result.Failure<TaskDto>(canManage.Error);
        }

        var taskResult = await GetTaskAsync(id, ct);
        if (taskResult.IsFailure)
        {
            return Result.Failure<TaskDto>(taskResult.Error);
        }

        var task = taskResult.Value;
        var sameOrganization = EnsureSameOrganization(task, user);
        if (sameOrganization.IsFailure)
        {
            return Result.Failure<TaskDto>(sameOrganization.Error);
        }

        task.Latitude = request.Latitude;
        task.Longitude = request.Longitude;
        task.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(task.ToDto());
    }

    public async Task<Result<TaskDto>> ChangeStatusAsync(Guid id, ChangeStatusRequest request, CurrentUser user, CancellationToken ct = default)
    {
        var taskResult = await GetTaskAsync(id, ct);
        if (taskResult.IsFailure)
        {
            return Result.Failure<TaskDto>(taskResult.Error);
        }

        var task = taskResult.Value;
        var canView = EnsureCanView(task, user);
        if (canView.IsFailure)
        {
            return Result.Failure<TaskDto>(canView.Error);
        }

        if (!user.IsOrgAdmin && task.AssigneeId != user.Id)
        {
            return Result.Failure<TaskDto>(Error.Forbidden("Only the assigned worker can change the status of this task."));
        }

        if (!Transitions.TryGetValue(user.Role, out var allowed) ||
            !allowed.Contains((task.Status, request.Status)))
        {
            return Result.Failure<TaskDto>(Error.BadRequest(
                $"Transition '{task.Status}' -> '{request.Status}' is not allowed for role '{user.Role}'."));
        }

        task.Status = request.Status;
        task.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(task.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid id, CurrentUser user, CancellationToken ct = default)
    {
        var canManage = EnsureOrgAdmin(user);
        if (canManage.IsFailure)
        {
            return canManage;
        }

        var taskResult = await GetTaskAsync(id, ct);
        if (taskResult.IsFailure)
        {
            return Result.Failure(taskResult.Error);
        }

        var task = taskResult.Value;
        var sameOrganization = EnsureSameOrganization(task, user);
        if (sameOrganization.IsFailure)
        {
            return sameOrganization;
        }

        unitOfWork.Tasks.Remove(task);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<CommentDto>> AddCommentAsync(Guid id, AddCommentRequest request, CurrentUser user, CancellationToken ct = default)
    {
        var taskResult = await GetTaskAsync(id, ct);
        if (taskResult.IsFailure)
        {
            return Result.Failure<CommentDto>(taskResult.Error);
        }

        var task = taskResult.Value;
        var canView = EnsureCanView(task, user);
        if (canView.IsFailure)
        {
            return Result.Failure<CommentDto>(canView.Error);
        }

        var author = await unitOfWork.Users.GetByIdAsync(user.Id, ct);
        if (author is null)
        {
            return Result.Failure<CommentDto>(Error.NotFound("Current user was not found."));
        }

        var comment = new TaskComment
        {
            TaskItemId = task.Id,
            AuthorId = author.Id,
            Author = author,
            Text = request.Text.Trim()
        };

        unitOfWork.Comments.Add(comment);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(comment.ToDto());
    }

    private async Task<Result<TaskItem>> GetTaskAsync(Guid id, CancellationToken ct)
    {
        var task = await unitOfWork.Tasks.GetDetailsAsync(id, ct);
        return task is null
            ? Result.Failure<TaskItem>(Error.NotFound($"Task '{id}' was not found."))
            : Result.Success(task);
    }

    private static Result EnsureOrgAdmin(CurrentUser user)
    {
        if (!user.IsOrgAdmin)
        {
            return Result.Failure(Error.Forbidden("Only organization administrators can perform this action."));
        }

        return Result.Success();
    }

    private static Result EnsureCanView(TaskItem task, CurrentUser user)
    {
        var sameOrganization = EnsureSameOrganization(task, user);
        if (sameOrganization.IsFailure)
        {
            return sameOrganization;
        }

        if (!user.IsOrgAdmin && task.AssigneeId != user.Id)
        {
            return Result.Failure(Error.Forbidden("Workers can only access tasks assigned to them."));
        }

        return Result.Success();
    }

    private async Task<Result> EnsureAssigneeIsWorkerAsync(Guid? assigneeId, Guid organizationId, CancellationToken ct)
    {
        if (assigneeId is null)
        {
            return Result.Success();
        }

        var assignee = await unitOfWork.Users.GetByIdAsync(assigneeId.Value, ct);
        if (assignee is null)
        {
            return Result.Failure(Error.BadRequest("Selected assignee does not exist."));
        }

        if (assignee.OrganizationId != organizationId || assignee.Role != UserRole.Worker || !assignee.IsActive)
        {
            return Result.Failure(Error.BadRequest("Tasks can only be assigned to active workers in the current organization."));
        }

        return Result.Success();
    }

    private static Result<Guid> RequireOrganization(CurrentUser user) =>
        user.OrganizationId is Guid organizationId
            ? Result.Success(organizationId)
            : Result.Failure<Guid>(Error.Forbidden("Current user is not linked to an organization."));

    private static Result EnsureSameOrganization(TaskItem task, CurrentUser user)
    {
        var organizationResult = RequireOrganization(user);
        if (organizationResult.IsFailure)
        {
            return Result.Failure(organizationResult.Error);
        }

        var organizationId = organizationResult.Value;
        if (task.OrganizationId != organizationId)
        {
            return Result.Failure(Error.NotFound($"Task '{task.Id}' was not found."));
        }

        return Result.Success();
    }

    private static DateTime? NormalizeUtc(DateTime? value) =>
        value is null
            ? null
            : value.Value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            };
}
