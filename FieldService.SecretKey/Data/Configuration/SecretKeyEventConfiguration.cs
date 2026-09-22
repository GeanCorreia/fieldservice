using FieldService.SecretKey.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.SecretKey.Data.Configuration;

internal sealed class SecretKeyEventConfiguration : IEntityTypeConfiguration<SecretKeyEvent>
{
    public void Configure(EntityTypeBuilder<SecretKeyEvent> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.OccurredAt)
            .IsRequired();

        builder.Property(x => x.SecretKeyId)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(x => x.TypeName)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.HasIndex(x => x.SecretKeyId);
        builder.HasIndex(x => new { x.SecretKeyId, x.OccurredAt });
    }
}

