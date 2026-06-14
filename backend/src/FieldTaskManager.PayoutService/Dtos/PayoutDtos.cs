using System.ComponentModel.DataAnnotations;

namespace FieldTaskManager.PayoutService.Dtos;

public sealed record PayoutAmountRequest(
    [Required, RegularExpression("^(USD|UAH)$")] string Currency,
    [Range(0, long.MaxValue)] long AmountMinor);

public sealed record CreatePayoutRequest(
    [Required] Guid UserId,
    [Required, MinLength(1)] IReadOnlyList<PayoutAmountRequest> Amounts);

public sealed record PayoutItemDto(
    Guid Id,
    string Currency,
    long AmountMinor,
    string Status,
    string? FailureReason);

public sealed record PayoutDto(
    Guid Id,
    Guid UserId,
    Guid OrganizationId,
    Guid RequestedById,
    string Status,
    DateTime RequestedAt,
    DateTime? CompletedAt,
    IReadOnlyList<PayoutItemDto> Items);

internal sealed record ApplyPayoutRequest(
    Guid PayoutId,
    Guid UserId,
    Guid OrganizationId,
    string Currency,
    long AmountMinor,
    DateTime? OccurredAt);
