using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace FieldService.Notification.Data.Configuration;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Entities.Notification>
{
    private static readonly JsonSerializerOptions MessageJsonOptions = new();

    public void Configure(EntityTypeBuilder<Entities.Notification> builder)
    {
        builder.ToTable("Notification");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.CreatedByUserId).IsRequired();
        builder.Ignore(n => n.Payload);
        builder.Property(n => n.Message)
            .HasColumnName("Message")
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(n => n.CreatedAt).IsRequired();
        builder.Property(n => n.ExpiresAt).IsRequired(false);
        builder.HasIndex(n => n.CreatedAt);

        builder.HasMany(n => n.Recipients)
            .WithOne()
            .HasForeignKey(r => r.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(n => n.Recipients)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
