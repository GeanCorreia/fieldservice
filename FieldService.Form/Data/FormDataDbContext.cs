using FieldService.Form.Entities;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Form.Data;

internal sealed class FormDataDbContext(DbContextOptions<FormDataDbContext> options) : DbContext(options)
{
    internal DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(FormTenant.DatabaseSchema);
        modelBuilder.Entity<FormSubmission>(entity =>
        {
            entity.ToTable(nameof(FormSubmission), FormTenant.DatabaseSchema);
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.FormId).IsRequired();
            entity.Property(x => x.DataJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.LastModifiedAt).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(x => x.IsDelete).HasDefaultValue(false).IsRequired();
            entity.Property(x => x.DeleteByUserId);
            entity.HasIndex(x => x.FormId);
            entity.HasIndex(x => new { x.FormId, x.IsDelete, x.LastModifiedAt });
        });
    }
}
