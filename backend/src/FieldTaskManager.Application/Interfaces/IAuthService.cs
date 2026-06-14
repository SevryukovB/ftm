using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Application.Common;

namespace FieldTaskManager.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
