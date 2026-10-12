using FieldService.Form.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Form.Data.Configuration;

// Maps to FormTenant entity (see: FieldService.Form.Entities.FormTenant)
internal class FormTenantConfiguration : IEntityTypeConfiguration<FormTenant>
{
    public void Configure(EntityTypeBuilder<FormTenant> builder)
    {
        builder.ToTable("FormTenant");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.ConnectionStringId)
            .IsRequired();

        builder.Property(x => x.DeveloperMcpEnabled)
            .IsRequired();

        builder.Property(x => x.FormExecutionEnabled)
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("IX_FormTenant_TenantId")
            .IsUnique();

        builder.HasIndex(x => x.ConnectionStringId)
            .HasDatabaseName("IX_FormTenant_ConnectionStringId");
    }
}

