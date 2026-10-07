using FieldService.Superset.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace FieldService.Superset.Data.Configurations;

internal sealed class SupersetTenantFlowConfiguration : IEntityTypeConfiguration<SupersetContainerDeploymentFlow>
{
    public void Configure(EntityTypeBuilder<SupersetContainerDeploymentFlow> builder)
    {
        builder.ToTable("SupersetTenantFlow");
        
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();
        
        builder.Property(x => x.CustomHostConnectionStringId)
            .IsRequired(false);

        builder.Property(x => x.StartedAt)
            .IsRequired();
        
        builder.Property(x => x.ContainerCreatedAt)
            .IsRequired(false);

        builder.Property(x => x.ContainerId)
            .IsRequired(false);

        builder.Property(x => x.SecretKeyId)
            .IsRequired(false);

        builder.Property(x => x.DataSchemaCreatedAt)
            .IsRequired(false);
        
        builder.Property(x => x.ConnectionStringId)
            .IsRequired(false);
        
        builder.Property(x => x.PersistedAt)
            .IsRequired(false);

        builder.Property(x => x.CancelledAt)
            .IsRequired(false);

        builder.Property(x => x.Configuration)
            .HasColumnType("jsonb")
            .HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => JsonSerializer.Deserialize<FieldService.Superset.Entities.SupersetContainerConfiguration>(value, (JsonSerializerOptions?)null)!);


        builder.Ignore(x => x.Status);
        
        builder.HasIndex(x => x.TenantId);
    }
}