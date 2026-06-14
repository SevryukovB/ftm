using System.ComponentModel.DataAnnotations;
using FieldTaskManager.Domain.Enums;

namespace FieldTaskManager.Application.Dtos;

public sealed record TaskDto(
    Guid Id,
    string Title,
    string Description,
    double Latitude,
    double Longitude,
    DateTime? Deadline,
    string Status,
    UserDto? Assignee,
    UserDto CreatedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<CommentDto>? Comments = null);

public sealed record CommentDto(Guid Id, string Text, UserDto Author, DateTime CreatedAt);

public sealed record CreateTaskRequest(
    [Required, MaxLength(200)] string Title,
    [MaxLength(4000)] string? Description,
    [Required, Range(-90, 90)] double Latitude,
    [Required, Range(-180, 180)] double Longitude,
    Guid? AssigneeId,
    DateTime? Deadline);

public sealed record UpdateTaskRequest(
    [Required, MaxLength(200)] string Title,
    [MaxLength(4000)] string? Description,
    [Required, Range(-90, 90)] double Latitude,
    [Required, Range(-180, 180)] double Longitude,
    Guid? AssigneeId,
    DateTime? Deadline);

public sealed record UpdateLocationRequest(
    [Required, Range(-90, 90)] double Latitude,
    [Required, Range(-180, 180)] double Longitude);

public sealed record ChangeStatusRequest([Required] FieldTaskStatus Status);

public sealed record AddCommentRequest([Required, MaxLength(2000)] string Text);
