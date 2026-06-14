using FieldTaskManager.EarningsService.Dtos;
using FieldTaskManager.EarningsService.Entities;
using FieldTaskManager.EarningsService.Extensions;
using FieldTaskManager.EarningsService.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FieldTaskManager.EarningsService.Controllers;

[ApiController]
[Authorize]
[Route("api/earnings")]
public sealed class EarningsController(EarningsDbContext context, IConfiguration configuration) : ControllerBase
{
    private static readonly string[] SupportedCurrencies = ["USD", "UAH"];

    [HttpGet("me/balance")]
    public async Task<ActionResult<BalanceSummaryDto>> MyBalance(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var balances = await context.Balances
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .ToListAsync(ct);

        return new BalanceSummaryDto(ToBalanceItems(balances));
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<ActionResult<IReadOnlyList<EarningStatisticsDto>>> OrganizationStatistics(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var organizationId = User.GetOrganizationId();
        var fromUtc = NormalizeUtc(from);
        var toUtc = NormalizeUtc(to);

        var transactionQuery = context.Transactions
            .AsNoTracking()
            .Where(t => t.OrganizationId == organizationId);

        if (fromUtc is not null)
        {
            transactionQuery = transactionQuery.Where(t => t.OccurredAt >= fromUtc);
        }

        if (toUtc is not null)
        {
            transactionQuery = transactionQuery.Where(t => t.OccurredAt < toUtc);
        }

        var transactionStats = await transactionQuery
            .GroupBy(t => new { t.UserId, t.Currency })
            .Select(g => new
            {
                g.Key.UserId,
                g.Key.Currency,
                EarnedAmountMinor = g.Where(t => t.Type == "TaskReward").Sum(t => t.AmountMinor),
                PaidAmountMinor = g.Where(t => t.Type == "Payout").Sum(t => -t.AmountMinor),
                VerifiedTasksCount = g.Count(t => t.Type == "TaskReward")
            })
            .ToListAsync(ct);

        var balances = await context.Balances
            .AsNoTracking()
            .Where(b => b.OrganizationId == organizationId)
            .ToListAsync(ct);

        var keys = transactionStats.Select(s => (s.UserId, s.Currency))
            .Concat(balances.Select(b => (b.UserId, b.Currency)))
            .Distinct()
            .OrderBy(k => k.UserId)
            .ThenBy(k => k.Currency)
            .ToList();

        return keys.Select(key =>
        {
            var tx = transactionStats.FirstOrDefault(s => s.UserId == key.UserId && s.Currency == key.Currency);
            var balance = balances.FirstOrDefault(b => b.UserId == key.UserId && b.Currency == key.Currency);
            return new EarningStatisticsDto(
                key.UserId,
                key.Currency,
                tx?.EarnedAmountMinor ?? 0,
                tx?.PaidAmountMinor ?? 0,
                balance?.AvailableAmountMinor ?? 0,
                tx?.VerifiedTasksCount ?? 0);
        }).ToList();
    }

    [HttpGet("users/{userId:guid}/task-history")]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<ActionResult<IReadOnlyList<TaskEarningHistoryDto>>> UserTaskHistory(
        Guid userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var organizationId = User.GetOrganizationId();
        var fromUtc = NormalizeUtc(from);
        var toUtc = NormalizeUtc(to);

        var query = context.Transactions
            .AsNoTracking()
            .Where(t => t.OrganizationId == organizationId && t.UserId == userId && t.Type == "TaskReward");

        if (fromUtc is not null)
        {
            query = query.Where(t => t.OccurredAt >= fromUtc);
        }

        if (toUtc is not null)
        {
            query = query.Where(t => t.OccurredAt < toUtc);
        }

        return await query
            .OrderByDescending(t => t.OccurredAt)
            .Select(t => new TaskEarningHistoryDto(
                t.Id,
                t.TaskId,
                t.TaskTitle ?? "Task",
                t.Currency,
                t.AmountMinor,
                t.OccurredAt))
            .ToListAsync(ct);
    }

    [HttpPost("internal/payouts/apply")]
    [AllowAnonymous]
    public async Task<IActionResult> ApplyPayout(
        ApplyPayoutRequest request,
        CancellationToken ct)
    {
        if (!IsInternalRequest())
        {
            return Unauthorized();
        }

        var currency = request.Currency.Trim().ToUpperInvariant();
        if (!SupportedCurrencies.Contains(currency))
        {
            return BadRequest(new { message = "Unsupported currency." });
        }

        var existing = await context.Transactions.AnyAsync(t => t.SourceEventId == request.PayoutId, ct);
        if (existing)
        {
            return NoContent();
        }

        var balance = await context.Balances.FindAsync([request.UserId, request.OrganizationId, currency], ct);
        if (balance is null || balance.AvailableAmountMinor < request.AmountMinor)
        {
            return BadRequest(new { message = "Insufficient balance." });
        }

        balance.AvailableAmountMinor -= request.AmountMinor;
        balance.PaidAmountMinor += request.AmountMinor;
        balance.UpdatedAt = DateTime.UtcNow;

        context.Transactions.Add(new EarningTransaction
        {
            SourceEventId = request.PayoutId,
            UserId = request.UserId,
            OrganizationId = request.OrganizationId,
            AmountMinor = -request.AmountMinor,
            Currency = currency,
            Type = "Payout",
            Status = "Confirmed",
            Description = "Mock payout released",
            OccurredAt = NormalizeUtc(request.OccurredAt) ?? DateTime.UtcNow
        });

        await context.SaveChangesAsync(ct);
        return NoContent();
    }

    private bool IsInternalRequest()
    {
        var expected = configuration["InternalApi:Key"];
        return !string.IsNullOrWhiteSpace(expected)
            && Request.Headers.TryGetValue("X-Internal-Api-Key", out var actual)
            && actual == expected;
    }

    private static IReadOnlyList<BalanceItemDto> ToBalanceItems(IReadOnlyList<EarningBalance> balances) =>
        SupportedCurrencies
            .Select(currency =>
            {
                var balance = balances.FirstOrDefault(b => b.Currency == currency);
                return new BalanceItemDto(
                    currency,
                    balance?.AvailableAmountMinor ?? 0,
                    balance?.PaidAmountMinor ?? 0);
            })
            .ToList();

    private static DateTime? NormalizeUtc(DateTime? value) =>
        value is null
            ? null
            : value.Value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            };
}
