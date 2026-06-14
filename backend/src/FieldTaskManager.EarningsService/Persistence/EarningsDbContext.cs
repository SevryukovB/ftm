using FieldTaskManager.EarningsService.Entities;
using Microsoft.EntityFrameworkCore;

namespace FieldTaskManager.EarningsService.Persistence;

public class EarningsDbContext(DbContextOptions<EarningsDbContext> options) : DbContext(options)
{
    public DbSet<EarningTransaction> Transactions => Set<EarningTransaction>();
    public DbSet<EarningBalance> Balances => Set<EarningBalance>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EarningTransaction>(builder =>
        {
            builder.ToTable("earning_transactions");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Currency).IsRequired().HasMaxLength(3);
            builder.Property(t => t.Type).IsRequired().HasMaxLength(32);
            builder.Property(t => t.Status).IsRequired().HasMaxLength(32);
            builder.Property(t => t.TaskTitle).HasMaxLength(200);
            builder.Property(t => t.Description).HasMaxLength(500);
            builder.HasIndex(t => t.SourceEventId).IsUnique();
            builder.HasIndex(t => new { t.OrganizationId, t.UserId, t.Currency, t.OccurredAt });
            builder.HasIndex(t => t.TaskId);
        });

        modelBuilder.Entity<EarningBalance>(builder =>
        {
            builder.ToTable("earning_balances");
            builder.HasKey(b => new { b.UserId, b.OrganizationId, b.Currency });
            builder.Property(b => b.Currency).HasMaxLength(3);
            builder.HasIndex(b => b.OrganizationId);
        });

        modelBuilder.Entity<ProcessedEvent>(builder =>
        {
            builder.ToTable("processed_events");
            builder.HasKey(e => e.EventId);
            builder.Property(e => e.Type).IsRequired().HasMaxLength(128);
        });
    }
}
