using System.ComponentModel.DataAnnotations;

namespace FieldTaskManager.EarningsService.Dtos;

public sealed record BalanceItemDto(string Currency, long AvailableAmountMinor, long PaidAmountMinor);

public sealed record BalanceSummaryDto(IReadOnlyList<BalanceItemDto> Balances);

public sealed record EarningStatisticsDto(
    Guid UserId,
    string Currency,
    long EarnedAmountMinor,
    long PaidAmountMinor,
    long AvailableAmountMinor,
    int VerifiedTasksCount);

public sealed record TaskEarningHistoryDto(
    Guid Id,
    Guid? TaskId,
    string TaskTitle,
    string Currency,
    long AmountMinor,
    DateTime OccurredAt);

public sealed record ApplyPayoutRequest(
    [Required] Guid PayoutId,
    [Required] Guid UserId,
    [Required] Guid OrganizationId,
    [Required, RegularExpression("^(USD|UAH)$")] string Currency,
    [Range(1, long.MaxValue)] long AmountMinor,
    DateTime? OccurredAt);
