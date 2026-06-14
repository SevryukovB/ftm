namespace FieldTaskManager.EarningsService.Entities;

public class EarningBalance
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Currency { get; set; } = "UAH";
    public long AvailableAmountMinor { get; set; }
    public long PaidAmountMinor { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
