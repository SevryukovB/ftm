using FieldTaskManager.Application.Common;
using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Application.Interfaces;
using FieldTaskManager.Application.Mapping;
using FieldTaskManager.Application.Messaging;
using FieldTaskManager.Domain.Entities;
using FieldTaskManager.Domain.Enums;
using FieldTaskManager.Domain.Repositories;
using System.Text.Json;

namespace FieldTaskManager.Application.Services;

public sealed class TaskService(IUnitOfWork unitOfWork) : ITaskService
{
    private static readonly JsonSerializerOptions EventJsonOptions = new(JsonSerializerDefaults.Web);

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

        var creator = await unitOfWork.Users.GetByIdAsync(user.Id, ct);
        if (creator is null)
        {
            return Result.Failure<TaskDto>(Error.NotFound("Current user was not found."));
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
            ReminderOffsetMinutes = request.ReminderOffsetMinutes,
            CreatedById = creator.Id
        };

        unitOfWork.Tasks.Add(task);
        AddTaskEvent("TaskCreated", task, new TaskCreatedEvent(
            task.Id,
            task.Title,
            task.OrganizationId,
            task.AssigneeId,
            assigneeResult.Value?.FullName,
            task.CreatedById,
            creator.FullName,
            task.Deadline,
            task.ReminderOffsetMinutes));
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
        task.ReminderOffsetMinutes = request.ReminderOffsetMinutes;
        task.UpdatedAt = DateTime.UtcNow;

        AddTaskEvent("TaskUpdated", task, new TaskUpdatedEvent(
            task.Id,
            task.Title,
            task.OrganizationId,
            task.AssigneeId,
            assigneeResult.Value?.FullName,
            task.CreatedById,
            task.CreatedBy.FullName,
            task.Status.ToString(),
            task.Deadline,
            task.ReminderOffsetMinutes));

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

        if (request.Status is FieldTaskStatus.Done or FieldTaskStatus.Verified)
        {
            AddTaskEvent(
                request.Status == FieldTaskStatus.Done ? "TaskDone" : "TaskVerified",
                task,
                new TaskStatusChangedEvent(
                    task.Id,
                    task.Title,
                    task.OrganizationId,
                    task.Status.ToString(),
                    task.AssigneeId,
                    task.Assignee?.FullName,
                    task.CreatedById,
                    task.CreatedBy.FullName,
                    user.Id,
                    task.Deadline,
                    task.ReminderOffsetMinutes));
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(task.ToDto());
    }

    public async Task<Result<TaskDto>> MarkNotCompletedAsync(Guid id, CancellationToken ct = default)
    {
        var taskResult = await GetTaskAsync(id, ct);
        if (taskResult.IsFailure)
        {
            return Result.Failure<TaskDto>(taskResult.Error);
        }

        var task = taskResult.Value;
        if (task.Status is FieldTaskStatus.Done or FieldTaskStatus.Verified or FieldTaskStatus.NotCompleted)
        {
            return Result.Success(task.ToDto());
        }

        if (task.Deadline is { } deadline && deadline > DateTime.UtcNow)
        {
            return Result.Failure<TaskDto>(Error.BadRequest("Task deadline has not passed yet."));
        }

        task.Status = FieldTaskStatus.NotCompleted;
        task.UpdatedAt = DateTime.UtcNow;

        AddTaskEvent(
            "TaskNotCompleted",
            task,
            new TaskStatusChangedEvent(
                task.Id,
                task.Title,
                task.OrganizationId,
                task.Status.ToString(),
                task.AssigneeId,
                task.Assignee?.FullName,
                task.CreatedById,
                task.CreatedBy.FullName,
                Guid.Empty,
                task.Deadline,
                task.ReminderOffsetMinutes));

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
        AddTaskEvent("TaskCommentAdded", task, new TaskCommentAddedEvent(
            task.Id,
            task.Title,
            task.OrganizationId,
            task.AssigneeId,
            task.Assignee?.FullName,
            task.CreatedById,
            task.CreatedBy.FullName,
            author.Id,
            author.FullName,
            comment.Text));
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

    private async Task<Result<User?>> EnsureAssigneeIsWorkerAsync(Guid? assigneeId, Guid organizationId, CancellationToken ct)
    {
        if (assigneeId is null)
        {
            return Result.Success<User?>(null);
        }

        var assignee = await unitOfWork.Users.GetByIdAsync(assigneeId.Value, ct);
        if (assignee is null)
        {
            return Result.Failure<User?>(Error.BadRequest("Selected assignee does not exist."));
        }

        if (assignee.OrganizationId != organizationId || assignee.Role != UserRole.Worker || !assignee.IsActive)
        {
            return Result.Failure<User?>(Error.BadRequest("Tasks can only be assigned to active workers in the current organization."));
        }

        return Result.Success<User?>(assignee);
    }

    private void AddTaskEvent(string type, TaskItem task, object payload)
    {
        var message = new OutboxMessage
        {
            Type = type,
            AggregateId = task.Id,
            OccurredAt = DateTime.UtcNow
        };
        message.Payload = JsonSerializer.Serialize(new OutboxEventEnvelope(
            message.Id,
            type,
            task.Id,
            message.OccurredAt,
            payload), EventJsonOptions);

        unitOfWork.Outbox.Add(message);
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
