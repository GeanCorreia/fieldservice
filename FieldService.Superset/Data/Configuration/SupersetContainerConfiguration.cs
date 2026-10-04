using System.Text.Json;
using FieldService.Superset.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Superset.Data.Configurations;

internal sealed class SupersetContainerConfiguration : IEntityTypeConfiguration<SupersetContainer>
{
    public void Configure(EntityTypeBuilder<SupersetContainer> builder)
    {
        builder.ToTable("SupersetContainer");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.SecretKeyId)
            .IsRequired();

        builder.Property(x => x.ProviderType)
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