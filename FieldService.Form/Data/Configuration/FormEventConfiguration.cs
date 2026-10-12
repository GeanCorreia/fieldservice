using FieldService.Form.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Form.Data.Configuration;

internal sealed class FormEventConfiguration : IEntityTypeConfiguration<FormEvent>
{
    public void Configure(EntityTypeBuilder<FormEvent> builder)
    {
        builder.ToTable("FormEvent");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.OccurredAt)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.FormId)
            .IsRequired();

        builder.Property(x => x.SchemaJson)
            .HasColumnType("jsonb");

        builder.Property(x => x.Title)
            .HasMaxLength(300);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.FormId)
            .HasDatabaseName("IX_FormEvent_FormId");

        builder.HasIndex(x => new { x.FormId, x.OccurredAt })
            .HasDatabaseName("IX_FormEvent_FormId_OccurredAt");
    }
}

