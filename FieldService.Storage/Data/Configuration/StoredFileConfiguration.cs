using System.Net.Http.Headers;
using FieldService.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldService.Storage.Data.Configuration;

internal sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("StoredFiles");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.FileCategoryId).IsRequired();
        builder.Property(x => x.UploadedByUserId).IsRequired();
        builder.Property(x => x.UploadedAt).IsRequired();
        builder.Property(x => x.HashMd5).IsRequired();
        builder.Property(x => x.FileName).IsRequired();
        builder.Property(x => x.Size).IsRequired();
        builder.Property(x => x.Provider).HasConversion<int>().IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.ContentType)
            .HasConversion(
                value => value.ToString(),
                value => MediaTypeHeaderValue.Parse(value))
            .HasColumnName("ContentType")
            .IsRequired();
        builder.Property(x => x.StoragePath).IsRequired();
        builder.Property(x => x.StatusChangedByUserId).IsRequired(false);
        builder.Property(x => x.StatusUpdatedAt).IsRequired(false);

        builder.HasIndex(x => x.FileCategoryId);
        builder.HasIndex(x => x.UploadedByUserId);
        builder.HasIndex(x => x.StoragePath).IsUnique();
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.FileCategory)
            .WithMany()
            .HasForeignKey(x => x.FileCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

