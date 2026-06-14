namespace FieldTaskManager.PayoutService.Entities;

public class Payout
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RequestedById { get; set; }
    public string Status { get; set; } = "Requested";
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public ICollection<PayoutItem> Items { get; set; } = new List<PayoutItem>();
}
