using FieldService.Notification.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Notification.Data.Configuration;

internal sealed class NotificationRecipientConfiguration : IEntityTypeConfiguration<NotificationRecipient>
{
    public void Configure(EntityTypeBuilder<NotificationRecipient> builder)
    {
        builder.ToTable("NotificationRecipient");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.NotificationId).IsRequired();
        builder.Property(r => r.Channel).IsRequired();
        builder.Property(r => r.UserId).IsRequired(false);
        builder.Property(r => r.Address)
            .HasMaxLength(320)
            .IsRequired();

        builder.HasMany(r => r.Events)
            .WithOne()
            .HasForeignKey(e => e.RecipientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.Events)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
