using FieldService.Superset.Entities;
using FieldService.Cloud.Entities;
using FieldService.Cloud.Data.Configuration;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Superset.Data;

public class SupersetDbContext(DbContextOptions<SupersetDbContext> options) : DbContext(options)
{
    internal DbSet<SupersetTenant> SupersetTenants => Set<SupersetTenant>();
    internal DbSet<SupersetTenantFlow> SupersetTenantFlows => Set<SupersetTenantFlow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SupersetDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new ContainerEntityConfiguration());
        modelBuilder.Entity<Container>().ToTable("Container", table => table.ExcludeFromMigrations());
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureContainersAreReadOnly();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnsureContainersAreReadOnly();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void EnsureContainersAreReadOnly()
    {
        if (ChangeTracker.Entries<Container>().Any(entry =>
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Containers must be persisted through the Cloud container repository.");
    }
}