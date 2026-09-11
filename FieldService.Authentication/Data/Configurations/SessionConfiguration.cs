using FieldService.Authentication.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Authentication.Data.Configurations;

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Session");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever();
        

        builder.Property(x => x.StartedAt)
            .HasColumnName("StartedAt")
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("ExpiresAt")
            .IsRequired();

        builder.Property(x => x.RevokedAt)
            .HasColumnName("RevokedAt");

        builder.Property(x => x.RevocationReason)
            .HasColumnName("RevocationReason")
            .HasConversion<int?>();

        builder.Property<Guid>("UserId")
            .HasColumnName("UserId")
            .IsRequired();
        
        builder.Property<Guid>("TenantId")
            .HasColumnName("TenantId")
            .IsRequired();
        
        builder.Property(x => x.ExternalId)
            .HasColumnName("ExternalId")
            .IsRequired();
        
        builder.Property(x => x.Provider)
            .HasColumnName("Provider")
            .HasConversion<int>()
            .IsRequired();
        
        builder.HasIndex("UserId", "TenantId")
            .HasDatabaseName("idx_Session_UserId_TenantId");
        
        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("idx_Session_ExpiresAt");
    }
}
