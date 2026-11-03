using Microsoft.EntityFrameworkCore;
using FieldService.AccountManagement.Events;

namespace FieldService.AccountManagement.Data;

public sealed class AccountManagementDbContext(DbContextOptions<AccountManagementDbContext> options) : DbContext(options)
{
    public DbSet<TenantEventBase> TenantEvents => Set<TenantEventBase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccountManagementDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
