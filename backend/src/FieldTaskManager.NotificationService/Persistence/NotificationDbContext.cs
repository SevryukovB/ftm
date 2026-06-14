using FieldTaskManager.NotificationService.Entities;
using Microsoft.EntityFrameworkCore;

namespace FieldTaskManager.NotificationService.Persistence;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> Preferences => Set<NotificationPreference>();
    public DbSet<DeliveryAttempt> DeliveryAttempts => Set<DeliveryAttempt>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(builder =>
        {
            builder.ToTable("notifications");
            builder.HasKey(n => n.Id);
            builder.Property(n => n.Type).IsRequired().HasMaxLength(128);
            builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
            builder.Property(n => n.Message).IsRequired().HasMaxLength(1000);
            builder.Property(n => n.PayloadJson).IsRequired();
            builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });
        });

        modelBuilder.Entity<NotificationPreference>(builder =>
        {
            builder.ToTable("notification_preferences");
            builder.HasKey(p => p.UserId);
        });

        modelBuilder.Entity<DeliveryAttempt>(builder =>
        {
            builder.ToTable("notification_delivery_attempts");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Channel).IsRequired().HasMaxLength(32);
            builder.Property(a => a.Status).IsRequired().HasMaxLength(32);
            builder.Property(a => a.Details).HasMaxLength(500);
            builder.HasIndex(a => a.NotificationId);
            builder.HasOne(a => a.Notification)
                .WithMany(n => n.DeliveryAttempts)
                .HasForeignKey(a => a.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProcessedEvent>(builder =>
        {
            builder.ToTable("processed_events");
            builder.HasKey(e => e.EventId);
            builder.Property(e => e.Type).IsRequired().HasMaxLength(128);
        });
    }
}
