using FieldTaskManager.PayoutService.Entities;
using Microsoft.EntityFrameworkCore;

namespace FieldTaskManager.PayoutService.Persistence;

public class PayoutDbContext(DbContextOptions<PayoutDbContext> options) : DbContext(options)
{
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<PayoutItem> PayoutItems => Set<PayoutItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payout>(builder =>
        {
            builder.ToTable("payouts");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Status).IsRequired().HasMaxLength(32);
            builder.HasIndex(p => new { p.OrganizationId, p.UserId, p.RequestedAt });
        });

        modelBuilder.Entity<PayoutItem>(builder =>
        {
            builder.ToTable("payout_items");
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Currency).IsRequired().HasMaxLength(3);
            builder.Property(i => i.Status).IsRequired().HasMaxLength(32);
            builder.Property(i => i.FailureReason).HasMaxLength(500);

            builder.HasOne(i => i.Payout)
                .WithMany(p => p.Items)
                .HasForeignKey(i => i.PayoutId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
