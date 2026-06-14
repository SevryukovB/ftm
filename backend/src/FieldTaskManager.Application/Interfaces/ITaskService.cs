using FieldTaskManager.Application.Common;
using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Domain.Repositories;

namespace FieldTaskManager.Application.Interfaces;

public interface ITaskService
{
    Task<IReadOnlyList<TaskDto>> SearchAsync(TaskFilter filter, CurrentUser user, CancellationToken ct = default);
    Task<TaskDto> GetAsync(Guid id, CurrentUser user, CancellationToken ct = default);
    Task<TaskDto> CreateAsync(CreateTaskRequest request, CurrentUser user, CancellationToken ct = default);
    Task<TaskDto> UpdateAsync(Guid id, UpdateTaskRequest request, CurrentUser user, CancellationToken ct = default);
    Task<TaskDto> UpdateLocationAsync(Guid id, UpdateLocationRequest request, CurrentUser user, CancellationToken ct = default);
    Task<TaskDto> ChangeStatusAsync(Guid id, ChangeStatusRequest request, CurrentUser user, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CurrentUser user, CancellationToken ct = default);
    Task<CommentDto> AddCommentAsync(Guid id, AddCommentRequest request, CurrentUser user, CancellationToken ct = default);
}
