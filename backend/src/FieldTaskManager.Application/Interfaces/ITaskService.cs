using FieldTaskManager.Application.Common;
using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Domain.Repositories;

namespace FieldTaskManager.Application.Interfaces;

public interface ITaskService
{
    Task<Result<IReadOnlyList<TaskDto>>> SearchAsync(TaskFilter filter, CurrentUser user, CancellationToken ct = default);
    Task<Result<TaskDto>> GetAsync(Guid id, CurrentUser user, CancellationToken ct = default);
    Task<Result<TaskDto>> CreateAsync(CreateTaskRequest request, CurrentUser user, CancellationToken ct = default);
    Task<Result<TaskDto>> UpdateAsync(Guid id, UpdateTaskRequest request, CurrentUser user, CancellationToken ct = default);
    Task<Result<TaskDto>> UpdateLocationAsync(Guid id, UpdateLocationRequest request, CurrentUser user, CancellationToken ct = default);
    Task<Result<TaskDto>> ChangeStatusAsync(Guid id, ChangeStatusRequest request, CurrentUser user, CancellationToken ct = default);
    Task<Result<TaskDto>> MarkNotCompletedAsync(Guid id, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CurrentUser user, CancellationToken ct = default);
    Task<Result<CommentDto>> AddCommentAsync(Guid id, AddCommentRequest request, CurrentUser user, CancellationToken ct = default);
}
