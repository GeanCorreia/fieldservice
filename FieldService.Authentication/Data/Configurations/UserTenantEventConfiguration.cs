using FieldService.Authentication.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Authentication.Data.Configurations;

internal sealed class UserTenantEventConfiguration : IEntityTypeConfiguration<UserTenantEvent>
{
    public void Configure(EntityTypeBuilder<UserTenantEvent> builder)
    {
        builder.ToTable("UserTenantEvent");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .HasColumnName("UserId")
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasColumnName("TenantId")
            .IsRequired();

        builder.Property(x => x.TenantName)
            .HasColumnName("TenantName")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasColumnName("CreatedBy")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("Status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.EndedAt)
            .HasColumnName("EndedAt");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("idx_UserTenantEvent_UserId");

        builder.HasIndex(x => new { x.UserId, x.TenantId, x.CreatedAt })
            .HasDatabaseName("idx_UserTenantEvent_UserId_TenantId_CreatedAt");
    }
}
