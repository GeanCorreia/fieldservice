using System.Text.Json;
using FieldService.Cloud.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Cloud.Data.Configuration;

public sealed class ContainerEntityConfiguration : IEntityTypeConfiguration<Container>
{
    public void Configure(EntityTypeBuilder<Container> builder)
    {
        builder.ToTable("Container");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.SecretKeyId)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique();

        builder.Property(x => x.CloudProvider)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ResourceId)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(x => x.FqdnUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ExecutionType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property<JsonElement?>("_executionWindow")
            .HasColumnName("ExecutionWindow")
            .HasColumnType("jsonb");

        builder.HasIndex(x => x.ResourceId)
            .IsUnique();
    }
}