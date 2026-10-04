using FieldService.Superset.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Superset.Data.Configurations;

internal sealed class SupersetTenantFlowConfiguration : IEntityTypeConfiguration<SupersetTenantFlow>
{
    public void Configure(EntityTypeBuilder<SupersetTenantFlow> builder)
    {
        builder.ToTable("SupersetTenantFlow");
        
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();
        
        builder.Property(x => x.FlowType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.StartedAt)
            .IsRequired();
        
        builder.Property(x => x.ContainerCreatedAt)
            .IsRequired(false);

        builder.Property(x => x.CreatedContainerId)
            .IsRequired(false);

        builder.Property(x => x.SecretKeyId)
            .IsRequired(false);

        builder.Property(x => x.DataSchemaCreatedAt)
            .IsRequired(false);

        builder.Property(x => x.RolesCreatedAt)
            .IsRequired(false);

        builder.Property(x => x.ConnectionStringId)
            .IsRequired(false);

        builder.Property(x => x.YamlMigrationFileId)
            .IsRequired(false);

        builder.Property(x => x.YamlMigrationFileCreatedAt)
            .IsRequired(false);

        builder.Property(x => x.YamlMigrationFileDeletedAt)
            .IsRequired(false);

        builder.Property(x => x.YamlMigrationFileUpdatedAt)
            .IsRequired(false);

        builder.Property(x => x.CompletedAt)
            .IsRequired(false);

        builder.Property(x => x.CancelledAt)
            .IsRequired(false);

        builder.OwnsOne(x => x.CreateParams, createParamsBuilder =>
        {
            createParamsBuilder.ToJson();
        });


        builder.Ignore(x => x.Status);
        
        builder.HasIndex(x => x.TenantId);
    }
}