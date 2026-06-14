using FieldTaskManager.Application.Dtos;
using FieldTaskManager.Domain.Entities;

namespace FieldTaskManager.Application.Mapping;

public static class DtoMapper
{
    public static UserDto ToDto(this User user) =>
        new(
            user.Id,
            user.Email,
            user.FullName,
            user.Role.ToString(),
            user.IsActive,
            user.OrganizationId,
            user.Organization?.Name);

    public static OrganizationDto ToDto(this Organization organization) =>
        new(
            organization.Id,
            organization.Name,
            organization.IsActive,
            organization.CreatedAt,
            organization.UpdatedAt);

    public static CommentDto ToDto(this TaskComment comment) =>
        new(comment.Id, comment.Text, comment.Author.ToDto(), comment.CreatedAt);

    public static TaskDto ToDto(this TaskItem task, bool includeComments = false) =>
        new(
            task.Id,
            task.Title,
            task.Description,
            task.Latitude,
            task.Longitude,
            task.Deadline,
            task.Status.ToString(),
            task.Assignee?.ToDto(),
            task.CreatedBy.ToDto(),
            task.CreatedAt,
            task.UpdatedAt,
            includeComments
                ? task.Comments.OrderBy(c => c.CreatedAt).Select(c => c.ToDto()).ToList()
                : null);
}
