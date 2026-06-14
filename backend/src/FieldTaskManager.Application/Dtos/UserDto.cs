namespace FieldTaskManager.Application.Dtos;

public sealed record UserDto(Guid Id, string Email, string FullName, string Role);
