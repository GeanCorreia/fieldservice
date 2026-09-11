using FieldService.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Storage.Data;

public sealed class StorageDbContext(DbContextOptions<StorageDbContext> options) : DbContext(options)
{
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();
    public DbSet<StoredFileCategory> StoredFileCategories => Set<StoredFileCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StorageDbContext).Assembly);
    }
}

