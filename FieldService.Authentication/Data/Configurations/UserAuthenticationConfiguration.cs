using FieldService.Authentication.Entities;
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

        builder.HasIndex(x => new { x.ExternalId, x.Provider })
            .HasDatabaseName("idx_UserAuthentication_ExternalId_Provider")
            .IsUnique();
    }
}
