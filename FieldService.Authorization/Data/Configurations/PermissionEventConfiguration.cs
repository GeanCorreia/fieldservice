using FieldService.Authorization.Events;
using FieldService.Shared.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Authorization.Data.Configurations;

public sealed class PermissionEventConfiguration : IEntityTypeConfiguration<PermissionEvent>
{
    public void Configure(EntityTypeBuilder<PermissionEvent> builder)
    {
        builder.ToTable("PermissionEvents");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever();
        
        builder.Property(e => e.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();
        
        builder.Property(e => e.CreatedBy)
            .HasColumnName("CreatedBy")
            .IsRequired();
        
        builder.Property(e => e.UserId)
            .HasColumnName("UserId")
            .IsRequired();
        
        builder.Property(e => e.TenantId)
            .HasColumnName("TenantId")
            .IsRequired();
        
        builder.Property(e => e.Type)
            .HasColumnName("Type")
            .HasConversion<int>()
            .IsRequired();

        builder.OwnsOne(e => e.Permission, navigationBuilder =>
        {
            navigationBuilder.Property(p => p.Module)
                .HasColumnName("PermissionModule")
                .IsRequired();

            navigationBuilder.Property(p => p.Resource)
                .HasColumnName("PermissionResource")
                .IsRequired();

            navigationBuilder.Property(p => p.Action)
                .HasColumnName("PermissionAction")
                .IsRequired();
        });

        builder.HasIndex(e => new { e.UserId, e.TenantId })
            .HasDatabaseName("idx_PermissionEvents_UserId_TenantId");
        
        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("idx_PermissionEvents_CreatedAt");
        
        builder.HasIndex(e => e.Type)
            .HasDatabaseName("idx_PermissionEvents_Type");
    }
}
