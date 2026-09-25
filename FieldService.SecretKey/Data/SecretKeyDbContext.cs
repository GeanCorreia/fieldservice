using FieldService.SecretKey.Events;
using Microsoft.EntityFrameworkCore;

namespace FieldService.SecretKey.Data;

public sealed class SecretKeyDbContext(DbContextOptions<SecretKeyDbContext> options) : DbContext(options)
{
    internal DbSet<Entities.SecretKey> SecretKeys => Set<Entities.SecretKey>();
    internal DbSet<SecretKeyEvent> SecretKeyEvents => Set<SecretKeyEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SecretKeyDbContext).Assembly);
    }
}