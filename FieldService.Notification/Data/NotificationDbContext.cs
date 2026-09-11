using Microsoft.EntityFrameworkCore;
using FieldService.Notification.Entities;
using FieldService.Notification.Events;

namespace FieldService.Notification.Data;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<Entities.Notification> Notifications => Set<Entities.Notification>();
    public DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();
    public DbSet<NotificationEvent> NotificationEvents => Set<NotificationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
