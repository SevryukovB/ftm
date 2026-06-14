using FieldTaskManager.Domain.Entities;

namespace FieldTaskManager.Application.Interfaces;

public interface ITokenService
{
    string CreateToken(User user);
}
