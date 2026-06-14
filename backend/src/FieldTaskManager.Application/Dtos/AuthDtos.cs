using System.ComponentModel.DataAnnotations;

namespace FieldTaskManager.Application.Dtos;

public sealed record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(128)] string FullName,
    [Required, MaxLength(160)] string OrganizationName,
    [Required, MinLength(6), MaxLength(128)] string Password);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record AuthResponse(string Token, UserDto User);
