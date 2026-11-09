using FieldService.Cloud.Events;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Cloud.Data;

public sealed class CloudDbContext(DbContextOptions<CloudDbContext> options) : DbContext(options)
{
    public DbSet<Entities.Container> Containers => Set<Entities.Container>();
    internal DbSet<Entities.SecretKey> SecretKeys => Set<Entities.SecretKey>();
    internal DbSet<SecretKeyEvent> SecretKeyEvents => Set<SecretKeyEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CloudDbContext).Assembly);
    }
}