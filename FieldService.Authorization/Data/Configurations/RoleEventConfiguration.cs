using FieldService.Authorization.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Authorization.Data.Configurations;

public sealed class RoleEventConfiguration : IEntityTypeConfiguration<RoleEvent>
{
    public void Configure(EntityTypeBuilder<RoleEvent> builder)
    {
        builder.ToTable("RoleEvents");
        
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
        
        builder.Property(e => e.Role)
            .HasColumnName("Role")
            .HasConversion<string>()
            .IsRequired();

        builder.HasIndex(e => new { e.UserId, e.TenantId })
            .HasDatabaseName("idx_RoleEvents_UserId_TenantId");
        
        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("idx_RoleEvents_CreatedAt");
    }
}
