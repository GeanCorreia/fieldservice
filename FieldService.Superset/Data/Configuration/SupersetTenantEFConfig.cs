using FieldService.Superset.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Superset.Data.Configuration;

internal class SupersetTenantEFConfig : IEntityTypeConfiguration<SupersetTenantConfig>
{
    public void Configure(EntityTypeBuilder<SupersetTenantConfig> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();
        
        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.ResourceId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.FqdnUrl)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.SupersetSecretKeyId)
            .IsRequired();

        builder.Property(x => x.ConnectionStringId)
            .IsRequired();
        
        builder.Property<string>("_instanceTier")
            .HasField("_instanceTier")
            .HasColumnName("InstanceTier")
            .HasColumnType("jsonb") 
            .IsRequired();

        builder.Property<string>("_executionWindow")
            .HasField("_executionWindow")
            .HasColumnName("ExecutionWindow")
            .HasColumnType("jsonb") 
            .IsRequired(false);
        
        builder.Ignore(x => x.InstanceTier);
        builder.Ignore(x => x.ExecutionWindow);
    }
}