using System.Text.Json;
using FiledService.Audit.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiledService.Audit.Data.Configurations;

public sealed class AuditAccessConfiguration : IEntityTypeConfiguration<AuditAccess>
{
    public void Configure(EntityTypeBuilder<AuditAccess> builder)
    {
        builder.ToTable("AuditAccesses");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever();

        builder.Property(e => e.RequestId)
            .HasColumnName("RequestId")
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasColumnName("UserId")
            .IsRequired();

        builder.Property(e => e.TenantId)
            .HasColumnName("TenantId")
            .IsRequired(false);

        builder.Property(e => e.OccurredAt)
            .HasColumnName("OccurredAt")
            .IsRequired();

        builder.Property(e => e.Resource)
            .HasColumnName("Resource")
            .IsRequired();
        
        builder.Property(e => e.ResourceId)
            .HasColumnName("ResourceId")
            .IsRequired(false);

        builder.Property<JsonDocument?>("_parameters")
            .HasColumnName("Parameters")
            .HasColumnType("jsonb");
        
        builder.Ignore(e => e.Parameters);

        builder.HasIndex(e => e.RequestId)
            .HasDatabaseName("idx_AuditAccesses_RequestId");

        builder.HasIndex(e => new { e.UserId, e.TenantId })
            .HasDatabaseName("idx_AuditAccesses_UserId_TenantId");

        builder.HasIndex(e => e.OccurredAt)
            .HasDatabaseName("idx_AuditAccesses_OccurredAt");
    }
}
