using FieldService.Notification.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Notification.Data.Configuration;

internal sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("notification_deliveries");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.NotificationId).IsRequired();
        builder.Property(d => d.UserId).IsRequired();
        builder.Property(d => d.DeviceId).IsRequired();
        builder.Property(d => d.ReceivedAt).IsRequired();
        builder.Property(d => d.ReadAt).IsRequired(false);
        
        builder.HasIndex(d => new { d.NotificationId, d.UserId, d.DeviceId, d.ReceivedAt });
        builder.HasAlternateKey(d => new { d.NotificationId, d.UserId, d.DeviceId })
            .HasName("AK_notification_deliveries_notification_user_device");
    }
}
