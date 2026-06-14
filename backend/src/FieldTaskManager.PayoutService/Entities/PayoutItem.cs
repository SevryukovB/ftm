namespace FieldTaskManager.PayoutService.Entities;

public class PayoutItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PayoutId { get; set; }
    public Payout Payout { get; set; } = null!;
    public string Currency { get; set; } = "UAH";
    public long AmountMinor { get; set; }
    public string Status { get; set; } = "Requested";
    public string? FailureReason { get; set; }
}
