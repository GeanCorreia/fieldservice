using FiledService.Audit.Entities;
using Microsoft.EntityFrameworkCore;

namespace FiledService.Audit.Data;

public sealed class AuditDbContext(DbContextOptions<AuditDbContext> options) : DbContext(options)
{
    public DbSet<AuditAccess> Accesses => Set<AuditAccess>();
    public DbSet<AuditChange> Changes => Set<AuditChange>();
    public DbSet<AuditDtoSchema> DtoSchemas => Set<AuditDtoSchema>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}