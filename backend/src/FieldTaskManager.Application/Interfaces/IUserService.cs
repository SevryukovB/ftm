using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Application.Common;

namespace FieldTaskManager.Application.Interfaces;

public interface IUserService
{
    Task<Result<IReadOnlyList<UserDto>>> ListAsync(CurrentUser currentUser, CancellationToken ct = default);
    Task<Result<IReadOnlyList<UserDto>>> ListWorkersAsync(CurrentUser currentUser, CancellationToken ct = default);
    Task<Result<UserDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CurrentUser currentUser, CancellationToken ct = default);
    Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request, CurrentUser currentUser, CancellationToken ct = default);
    Task<Result> DeactivateAsync(Guid id, CurrentUser currentUser, CancellationToken ct = default);
}
