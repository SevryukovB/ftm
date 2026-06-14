using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Application.Common;

namespace FieldTaskManager.Application.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> ListAsync(CurrentUser currentUser, CancellationToken ct = default);
    Task<IReadOnlyList<UserDto>> ListWorkersAsync(CurrentUser currentUser, CancellationToken ct = default);
    Task<UserDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<UserDto> CreateAsync(CreateUserRequest request, CurrentUser currentUser, CancellationToken ct = default);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CurrentUser currentUser, CancellationToken ct = default);
    Task DeactivateAsync(Guid id, CurrentUser currentUser, CancellationToken ct = default);
}
