using FieldTaskManager.Application.Dtos;

namespace FieldTaskManager.Application.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> ListWorkersAsync(CancellationToken ct = default);
    Task<UserDto> GetAsync(Guid id, CancellationToken ct = default);
}
