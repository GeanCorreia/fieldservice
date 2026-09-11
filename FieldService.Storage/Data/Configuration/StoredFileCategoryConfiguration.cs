using System.Text.Json;
using FieldService.Shared.Types;
using FieldService.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Storage.Data.Configuration;

internal sealed class StoredFileCategoryConfiguration : IEntityTypeConfiguration<StoredFileCategory>
{
    public void Configure(EntityTypeBuilder<StoredFileCategory> builder)
    {
        builder.ToTable("StoredFileCategories");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Code).IsRequired();
        builder.Property(x => x.MaxSizeInBytes).IsRequired(false);
        builder.Property(x => x.Version)
            .HasConversion(
                value => value.ToString(),
                value => SchemaVersion.FromString(value))
            .HasColumnName("Version")
            .IsRequired();
        builder.Property(x => x.MinimumRequiredRole)
            .HasConversion<int?>()
            .IsRequired(false);

        builder.Ignore(x => x.AllowedContentTypes);
        builder.Ignore(x => x.AllowedPermissions);

        builder.Property<JsonElement>("_allowedContentTypes")
            .HasColumnName("AllowedContentTypes")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property<JsonElement>("_allowedPermissions")
            .HasColumnName("AllowedPermissions")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        builder.HasIndex(x => x.TenantId);
    }
}

