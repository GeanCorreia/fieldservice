using FieldService.Form.Entities;
using FieldService.Form.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Form.Data.Configuration;

// Maps to Form entity (see: FieldService.Form.Entities.Form)
internal class FormConfiguration : IEntityTypeConfiguration<global::FieldService.Form.Entities.Form>
{
    public void Configure(EntityTypeBuilder<global::FieldService.Form.Entities.Form> builder)
    {
        builder.ToTable("Form");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.HasMany<FormEvent>("_events")
            .WithOne()
            .HasForeignKey(x => x.FormId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("_events")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("IX_Form_TenantId");

        builder.HasIndex(x => new { x.TenantId, x.Id })
            .HasDatabaseName("IX_Form_TenantId_Id");
    }
}
