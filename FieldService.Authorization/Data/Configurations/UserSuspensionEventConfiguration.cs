using FieldService.Authorization.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Authorization.Data.Configurations;

public sealed class UserSuspensionEventConfiguration : IEntityTypeConfiguration<UserSuspensionEvent>
{
    public void Configure(EntityTypeBuilder<UserSuspensionEvent> builder)
    {
        builder.ToTable("UserSuspensionEvents");
        
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
        
        builder.Property(e => e.Source)
            .HasColumnName("Source")
            .HasConversion<int?>();
        
        builder.Property(e => e.StartedAt)
            .HasColumnName("StartedAt");
        
        builder.Property(e => e.EndedAt)
            .HasColumnName("EndedAt");
        
        builder.Property(e => e.OriginalSuspensionEventId)
            .HasColumnName("OriginalSuspensionEventId");

        builder.HasIndex(e => new { e.UserId, e.TenantId })
            .HasDatabaseName("idx_UserSuspensionEvents_UserId_TenantId");
        
        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("idx_UserSuspensionEvents_CreatedAt");
        
        builder.HasIndex(e => e.Type)
            .HasDatabaseName("idx_UserSuspensionEvents_Type");
        
        builder.HasIndex(e => e.OriginalSuspensionEventId)
            .HasDatabaseName("idx_UserSuspensionEvents_OriginalSuspensionEventId");
    }
}
