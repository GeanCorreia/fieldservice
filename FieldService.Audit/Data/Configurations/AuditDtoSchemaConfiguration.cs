using System.Text.Json;
using FiledService.Audit.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiledService.Audit.Data.Configurations;

public sealed class AuditDtoSchemaConfiguration : IEntityTypeConfiguration<AuditDtoSchema>
{
    public void Configure(EntityTypeBuilder<AuditDtoSchema> builder)
    {
        builder.ToTable("AuditDtoSchemas");

        builder.HasKey(e => new { e.ResourceName, e.Version });

        builder.Property(e => e.ResourceName)
            .HasColumnName("ResourceName")
            .IsRequired();

        builder.Property(e => e.Version)
            .HasColumnName("Version")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Ignore(e => e.Properties);
        builder.Property<JsonDocument>("_properties")
            .HasColumnName("Properties")
            .HasColumnType("jsonb")
            .IsRequired();
    }
}
