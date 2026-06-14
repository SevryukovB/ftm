namespace FieldTaskManager.Application.Dtos;

public sealed record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    bool IsActive,
    Guid? OrganizationId,
    string? OrganizationName);

public sealed record CreateUserRequest(
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress, System.ComponentModel.DataAnnotations.MaxLength(256)] string Email,
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(128)] string FullName,
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MinLength(6), System.ComponentModel.DataAnnotations.MaxLength(128)] string Password,
    [System.ComponentModel.DataAnnotations.Required] string Role);

public sealed record UpdateUserRequest(
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(128)] string FullName,
    [System.ComponentModel.DataAnnotations.Required] string Role,
    bool IsActive);
