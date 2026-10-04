using FieldService.Superset.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Superset.Data.Configurations;

internal sealed class SupersetTenantConfiguration : IEntityTypeConfiguration<SupersetTenant>
{
    public void Configure(EntityTypeBuilder<SupersetTenant> builder)
    {
        builder.ToTable("SupersetTenant");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.ContainerId)
            .IsRequired();

        builder.Property(x => x.ConnectionStringId)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.StatusUpdatedAt)
            .IsRequired();

        builder.HasOne(x => x.Container)
            .WithMany()
            .HasForeignKey(x => x.ContainerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.TenantId)
            .IsUnique();
    }
}