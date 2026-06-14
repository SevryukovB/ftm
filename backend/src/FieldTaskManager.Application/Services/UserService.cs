using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Application.Exceptions;
using FieldTaskManager.Application.Interfaces;
using FieldTaskManager.Application.Mapping;
using FieldTaskManager.Domain.Enums;
using FieldTaskManager.Domain.Repositories;

namespace FieldTaskManager.Application.Services;

public sealed class UserService(IUnitOfWork unitOfWork) : IUserService
{
    public async Task<IReadOnlyList<UserDto>> ListWorkersAsync(CancellationToken ct = default)
    {
        var workers = await unitOfWork.Users.ListByRoleAsync(UserRole.Worker, ct);
        return workers.Select(w => w.ToDto()).ToList();
    }

    public async Task<UserDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var user = await unitOfWork.Users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"User '{id}' was not found.");
        return user.ToDto();
    }
}
