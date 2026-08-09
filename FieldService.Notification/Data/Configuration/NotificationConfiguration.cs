using System.Text.Json;
using FieldService.Shared.Types;
using FieldService.SignalR.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FieldService.Notification.Data.Configuration;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Entities.Notification>
{
    private static readonly ValueConverter<JsonElement, string> PayloadConverter =
        new(
            value => value.GetRawText(),
            value => JsonDocument.Parse(value, new JsonDocumentOptions()).RootElement.Clone());

    private static readonly ValueConverter<SchemaVersion, string> VersionConverter =
        new(
            value => value.ToString(),
            value => SchemaVersion.FromString(value));

    public void Configure(EntityTypeBuilder<Entities.Notification> builder)
    {
        builder.ToTable("Notification");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.OwnsOne(n => n.TargetContext, target =>
        {
            target.Property(t => t.TargetType)
                .HasColumnName("TargetType")
                .HasConversion<string>()
                .IsRequired();

            target.Property(t => t.TargetId)
                .HasColumnName("TargetId")
                .HasMaxLength(100)
                .IsRequired(false);

            target.Property(t => t.MessageType)
                .HasColumnName("TargetMessageType")
                .HasMaxLength(200)
                .IsRequired();

            target.Property(t => t.SchemaVersion)
                .HasColumnName("TargetVersion")
                .HasConversion(VersionConverter)
                .HasMaxLength(32)
                .IsRequired();
        });

        builder.Navigation(n => n.TargetContext).IsRequired();

        builder.Property(n => n.Payload)
            .HasConversion(PayloadConverter)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(n => n.CreatedAt).IsRequired();
        builder.Property(n => n.DeletedAt).IsRequired(false);
        builder.HasIndex(n => n.CreatedAt);

        builder.HasMany(n => n.Deliveries)
            .WithOne()
            .HasForeignKey(d => d.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(n => n.Deliveries)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
