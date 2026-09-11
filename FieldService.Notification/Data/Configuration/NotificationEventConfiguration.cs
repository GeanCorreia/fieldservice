using FieldService.Notification.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Notification.Data.Configuration;

internal sealed class NotificationEventConfiguration : IEntityTypeConfiguration<NotificationEvent>
{
    public void Configure(EntityTypeBuilder<NotificationEvent> builder)
    {
        builder.ToTable("NotificationEvent");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.RecipientId).IsRequired();
        builder.Property(e => e.OccurredAt).IsRequired();
        builder.Property(e => e.Type).IsRequired();
        builder.Property(e => e.FailureReason).IsRequired(false);
        builder.Property(e => e.CanceledByUserId).IsRequired(false);

        builder.HasIndex(e => new { e.RecipientId, e.OccurredAt });
    }
}
