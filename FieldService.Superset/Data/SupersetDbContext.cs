using Microsoft.EntityFrameworkCore;

namespace FieldService.Superset.Data;

public class SupersetDbContext(DbContextOptions<SupersetDbContext> options) : DbContext(options)
{
    internal DbSet<Entities.SupersetTenantConfig> SupersetTenantConfigs => Set<Entities.SupersetTenantConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SupersetDbContext).Assembly);
    }
    
}