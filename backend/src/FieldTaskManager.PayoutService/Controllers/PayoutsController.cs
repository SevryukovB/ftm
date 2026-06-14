using System.Net.Http.Json;
using FieldTaskManager.PayoutService.Dtos;
using FieldTaskManager.PayoutService.Entities;
using FieldTaskManager.PayoutService.Extensions;
using FieldTaskManager.PayoutService.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FieldTaskManager.PayoutService.Controllers;

[ApiController]
[Authorize]
[Route("api/payouts")]
public sealed class PayoutsController(
    PayoutDbContext context,
    IHttpClientFactory httpClientFactory,
    ILogger<PayoutsController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<ActionResult<IReadOnlyList<PayoutDto>>> List(
        [FromQuery] Guid? userId,
        CancellationToken ct)
    {
        var organizationId = User.GetOrganizationId();
        var query = context.Payouts
            .AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.OrganizationId == organizationId);

        if (userId is not null)
        {
            query = query.Where(p => p.UserId == userId);
        }

        var payouts = await query
            .OrderByDescending(p => p.RequestedAt)
            .Take(100)
            .ToListAsync(ct);

        return payouts.Select(ToDto).ToList();
    }

    [HttpPost]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<ActionResult<PayoutDto>> Create(CreatePayoutRequest request, CancellationToken ct)
    {
        var organizationId = User.GetOrganizationId();
        var requestedById = User.GetUserId();
        var amounts = request.Amounts
            .Select(a => new
            {
                Currency = a.Currency.Trim().ToUpperInvariant(),
                a.AmountMinor
            })
            .Where(a => a.AmountMinor > 0)
            .GroupBy(a => a.Currency)
            .Select(g => new { Currency = g.Key, AmountMinor = g.Sum(x => x.AmountMinor) })
            .Where(a => a.Currency is "USD" or "UAH")
            .ToList();

        if (amounts.Count == 0)
        {
            return BadRequest(new { message = "Enter at least one positive payout amount." });
        }

        var payout = new Payout
        {
            UserId = request.UserId,
            OrganizationId = organizationId,
            RequestedById = requestedById,
            Status = "Processing",
            Items = amounts.Select(a => new PayoutItem
            {
                Currency = a.Currency,
                AmountMinor = a.AmountMinor,
                Status = "Processing"
            }).ToList()
        };

        context.Payouts.Add(payout);
        await context.SaveChangesAsync(ct);

        var client = httpClientFactory.CreateClient("earnings");
        var completed = true;

        foreach (var item in payout.Items)
        {
            try
            {
                var response = await client.PostAsJsonAsync(
                    "/api/earnings/internal/payouts/apply",
                    new ApplyPayoutRequest(
                        item.Id,
                        payout.UserId,
                        payout.OrganizationId,
                        item.Currency,
                        item.AmountMinor,
                        DateTime.UtcNow),
                    ct);

                if (response.IsSuccessStatusCode)
                {
                    item.Status = "Completed";
                    continue;
                }

                item.Status = "Failed";
                item.FailureReason = await response.Content.ReadAsStringAsync(ct);
                completed = false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Mock payout item {PayoutItemId} failed.", item.Id);
                item.Status = "Failed";
                item.FailureReason = "Earnings service is unavailable.";
                completed = false;
            }
        }

        payout.Status = completed ? "Completed" : "Failed";
        payout.CompletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        var dto = ToDto(payout);
        return completed ? Ok(dto) : BadRequest(dto);
    }

    private static PayoutDto ToDto(Payout payout) =>
        new(
            payout.Id,
            payout.UserId,
            payout.OrganizationId,
            payout.RequestedById,
            payout.Status,
            payout.RequestedAt,
            payout.CompletedAt,
            payout.Items
                .OrderBy(i => i.Currency)
                .Select(i => new PayoutItemDto(i.Id, i.Currency, i.AmountMinor, i.Status, i.FailureReason))
                .ToList());
}
