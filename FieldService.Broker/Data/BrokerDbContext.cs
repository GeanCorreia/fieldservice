using FieldService.Broker.Entities;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Broker.Data;

public sealed class BrokerDbContext(DbContextOptions<BrokerDbContext> options) : DbContext(options)
{
    public DbSet<BrokerOutbox> BrokerOutboxes => Set<BrokerOutbox>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BrokerDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
