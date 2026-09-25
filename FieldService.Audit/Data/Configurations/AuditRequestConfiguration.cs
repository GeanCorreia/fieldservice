using FieldService.Audit.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Audit.Data.Configurations;

internal sealed class SessionActivityConfiguration : IEntityTypeConfiguration<AuditRequest>
{
    public void Configure(EntityTypeBuilder<AuditRequest> builder)
    {
        builder.ToTable("AuditRequest");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever();

        builder.Property(x => x.JwtId)
            .HasColumnName("JwtId")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.IpAddressHash)
            .HasColumnName("IpAddressHash")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.UserAgentHash)
            .HasColumnName("UserAgentHash")
            .HasMaxLength(256);

        builder.Property(x => x.Timestamp)
            .HasColumnName("Timestamp")
            .IsRequired();

        builder.Property(x => x.Channel)
            .HasColumnName("Channel")
            .HasConversion<int>()
            .IsRequired();
        
        
        builder.Property(x => x.Resource)
            .HasColumnName("Resource")
            .HasMaxLength(512)
            .IsRequired();
        
        builder.Property(x => x.Successful)
            .HasColumnName("Successful")
            .IsRequired();
        
        builder.Property(x => x.StatusCode)
            .HasColumnName("StatusCode")
            .IsRequired();
        
        builder.Property(x => x.UserId)
            .HasColumnName("UserId")
            .IsRequired();

        builder.Property(x => x.SessionId)
            .HasColumnName("SessionId");

        builder.HasIndex(x => x.Timestamp)
            .HasDatabaseName("idx_SessionActivity_Timestamp");
    }
}
