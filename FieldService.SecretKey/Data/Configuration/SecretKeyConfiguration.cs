using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.SecretKey.Data.Configuration;

internal sealed class SecretKeyConfiguration : IEntityTypeConfiguration<Entities.SecretKey>
{
    public void Configure(EntityTypeBuilder<Entities.SecretKey> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Ignore(x => x.Reference);
        builder.Ignore(x => x.History);
        builder.Ignore(x => x.SecretKeyEvents);
        builder.Ignore(x => x.IsDeleted);

        builder.HasMany<Events.SecretKeyEvent>("_secretKeyEvents")
            .WithOne()
            .HasForeignKey(x => x.SecretKeyId)
            .HasPrincipalKey(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("_secretKeyEvents")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.TenantId);

        builder.HasIndex(x => new { x.TenantId, x.Type });
    }
}