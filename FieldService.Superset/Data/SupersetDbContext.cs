using FieldService.Superset.Entities;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Superset.Data;

public class SupersetDbContext(DbContextOptions<SupersetDbContext> options) : DbContext(options)
{
    internal DbSet<SupersetTenant> SupersetTenants => Set<SupersetTenant>();
    internal DbSet<SupersetContainer> SupersetContainers => Set<SupersetContainer>();
    internal DbSet<SupersetContainerDeploymentFlow> SupersetTenantFlows => Set<SupersetContainerDeploymentFlow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SupersetDbContext).Assembly);
    }
    
}