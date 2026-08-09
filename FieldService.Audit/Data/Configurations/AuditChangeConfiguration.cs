using System.Text.Json;
using FiledService.Audit.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiledService.Audit.Data.Configurations;

public sealed class AuditChangeConfiguration : IEntityTypeConfiguration<AuditChange>
{
    public void Configure(EntityTypeBuilder<AuditChange> builder)
    {
        builder.ToTable("AuditChanges");

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
            .IsRequired();

        builder.Property(e => e.OccurredAt)
            .HasColumnName("OccurredAt")
            .IsRequired();

        builder.Property(e => e.Resource)
            .HasColumnName("Resource")
            .IsRequired();

        builder.Property(e => e.ResourceId)
            .HasColumnName("ResourceId")
            .IsRequired();
    
        builder.Ignore(e => e.ChangedProperties);
        
        builder.Property<JsonDocument?>("_changedProperties")
            .HasColumnName("ChangedProperties")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(e => e.RequestId)
            .HasDatabaseName("idx_AuditChanges_RequestId");

        builder.HasIndex(e => new { e.UserId, e.TenantId })
            .HasDatabaseName("idx_AuditChanges_UserId_TenantId");

        builder.HasIndex(e => e.OccurredAt)
            .HasDatabaseName("idx_AuditChanges_OccurredAt");
    }
}
