using FieldService.Authentication.Entities;
using FieldService.Authentication.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Authentication.Data.Configurations;

internal sealed class UserAuthenticationConfiguration : IEntityTypeConfiguration<UserAuthentication>
{
    public void Configure(EntityTypeBuilder<UserAuthentication> builder)
    {
        builder.ToTable("UserAuthentication");

        builder.HasKey(x => x.UserId);

        builder.Property(x => x.UserId)
            .HasColumnName("UserId")
            .ValueGeneratedNever();

        builder.Property(x => x.ExternalId)
            .HasColumnName("ExternalId")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Provider)
            .HasColumnName("Provider")
            .HasConversion<int>()
            .IsRequired();

        builder.Ignore(x => x.Tenants);

        builder.HasMany<UserTenantEvent>("_events")
            .WithOne()
            .HasForeignKey(x => x.UserId)
            .HasPrincipalKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("_events")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => new { x.ExternalId, x.Provider })
            .HasDatabaseName("idx_UserAuthentication_ExternalId_Provider")
            .IsUnique();
    }
}
