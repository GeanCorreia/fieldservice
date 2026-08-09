using FieldService.Authentication.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Authentication.Data.Configurations;

internal sealed class SessionActivityConfiguration : IEntityTypeConfiguration<SessionActivity>
{
    public void Configure(EntityTypeBuilder<SessionActivity> builder)
    {
        builder.ToTable("SessionActivity");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever();

        builder.Property(x => x.SessionId)
            .HasColumnName("SessionId")
            .IsRequired();

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

        builder.Property(x => x.RequestId)
            .HasColumnName("RequestId")
            .IsRequired();

        builder.HasIndex(x => x.SessionId)
            .HasDatabaseName("idx_SessionActivity_SessionId");

        builder.HasIndex(x => x.Timestamp)
            .HasDatabaseName("idx_SessionActivity_Timestamp");
    }
}
