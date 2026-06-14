using FieldTaskManager.Application.Common;
using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Application.Exceptions;
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

    public async Task<IReadOnlyList<TaskDto>> SearchAsync(TaskFilter filter, CurrentUser user, CancellationToken ct = default)
    {
        var organizationId = RequireOrganization(user);
        var effectiveFilter = user.IsOrgAdmin ? filter : filter with { AssigneeId = user.Id };
        var tasks = await unitOfWork.Tasks.SearchAsync(organizationId, effectiveFilter, ct);
        return tasks.Select(t => t.ToDto()).ToList();
    }

    public async Task<TaskDto> GetAsync(Guid id, CurrentUser user, CancellationToken ct = default)
    {
        var task = await GetTaskOrThrowAsync(id, ct);
        EnsureCanView(task, user);
        return task.ToDto(includeComments: true);
    }

    public async Task<TaskDto> CreateAsync(CreateTaskRequest request, CurrentUser user, CancellationToken ct = default)
    {
        EnsureOrgAdmin(user);
        var organizationId = RequireOrganization(user);
        await EnsureAssigneeIsWorkerAsync(request.AssigneeId, organizationId, ct);

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

        return (await GetTaskOrThrowAsync(task.Id, ct)).ToDto();
    }

    public async Task<TaskDto> UpdateAsync(Guid id, UpdateTaskRequest request, CurrentUser user, CancellationToken ct = default)
    {
        EnsureOrgAdmin(user);
        var organizationId = RequireOrganization(user);
        await EnsureAssigneeIsWorkerAsync(request.AssigneeId, organizationId, ct);

        var task = await GetTaskOrThrowAsync(id, ct);
        EnsureSameOrganization(task, user);

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim() ?? string.Empty;
        task.Latitude = request.Latitude;
        task.Longitude = request.Longitude;
        task.AssigneeId = request.AssigneeId;
        task.Deadline = NormalizeUtc(request.Deadline);
        task.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(ct);
        return (await GetTaskOrThrowAsync(id, ct)).ToDto();
    }

    public async Task<TaskDto> UpdateLocationAsync(Guid id, UpdateLocationRequest request, CurrentUser user, CancellationToken ct = default)
    {
        EnsureOrgAdmin(user);

        var task = await GetTaskOrThrowAsync(id, ct);
        EnsureSameOrganization(task, user);
        task.Latitude = request.Latitude;
        task.Longitude = request.Longitude;
        task.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(ct);
        return task.ToDto();
    }

    public async Task<TaskDto> ChangeStatusAsync(Guid id, ChangeStatusRequest request, CurrentUser user, CancellationToken ct = default)
    {
        var task = await GetTaskOrThrowAsync(id, ct);
        EnsureCanView(task, user);

        if (!user.IsOrgAdmin && task.AssigneeId != user.Id)
        {
            throw new ForbiddenException("Only the assigned worker can change the status of this task.");
        }

        var allowed = Transitions[user.Role];
        if (!allowed.Contains((task.Status, request.Status)))
        {
            throw new BadRequestException(
                $"Transition '{task.Status}' -> '{request.Status}' is not allowed for role '{user.Role}'.");
        }

        task.Status = request.Status;
        task.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(ct);
        return task.ToDto();
    }

    public async Task DeleteAsync(Guid id, CurrentUser user, CancellationToken ct = default)
    {
        EnsureOrgAdmin(user);
        var task = await GetTaskOrThrowAsync(id, ct);
        EnsureSameOrganization(task, user);
        unitOfWork.Tasks.Remove(task);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<CommentDto> AddCommentAsync(Guid id, AddCommentRequest request, CurrentUser user, CancellationToken ct = default)
    {
        var task = await GetTaskOrThrowAsync(id, ct);
        EnsureCanView(task, user);

        var author = await unitOfWork.Users.GetByIdAsync(user.Id, ct)
            ?? throw new NotFoundException("Current user was not found.");

        var comment = new TaskComment
        {
            TaskItemId = task.Id,
            AuthorId = author.Id,
            Author = author,
            Text = request.Text.Trim()
        };

        unitOfWork.Comments.Add(comment);
        await unitOfWork.SaveChangesAsync(ct);

        return comment.ToDto();
    }

    private async Task<TaskItem> GetTaskOrThrowAsync(Guid id, CancellationToken ct) =>
        await unitOfWork.Tasks.GetDetailsAsync(id, ct)
            ?? throw new NotFoundException($"Task '{id}' was not found.");

    private static void EnsureOrgAdmin(CurrentUser user)
    {
        if (!user.IsOrgAdmin)
        {
            throw new ForbiddenException("Only organization administrators can perform this action.");
        }
    }

    private static void EnsureCanView(TaskItem task, CurrentUser user)
    {
        EnsureSameOrganization(task, user);

        if (!user.IsOrgAdmin && task.AssigneeId != user.Id)
        {
            throw new ForbiddenException("Workers can only access tasks assigned to them.");
        }
    }

    private async Task EnsureAssigneeIsWorkerAsync(Guid? assigneeId, Guid organizationId, CancellationToken ct)
    {
        if (assigneeId is null)
        {
            return;
        }

        var assignee = await unitOfWork.Users.GetByIdAsync(assigneeId.Value, ct)
            ?? throw new BadRequestException("Selected assignee does not exist.");

        if (assignee.OrganizationId != organizationId || assignee.Role != UserRole.Worker || !assignee.IsActive)
        {
            throw new BadRequestException("Tasks can only be assigned to active workers in the current organization.");
        }
    }

    private static Guid RequireOrganization(CurrentUser user) =>
        user.OrganizationId ?? throw new ForbiddenException("Current user is not linked to an organization.");

    private static void EnsureSameOrganization(TaskItem task, CurrentUser user)
    {
        var organizationId = RequireOrganization(user);
        if (task.OrganizationId != organizationId)
        {
            throw new NotFoundException($"Task '{task.Id}' was not found.");
        }
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
