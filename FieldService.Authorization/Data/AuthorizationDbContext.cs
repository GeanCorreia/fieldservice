using FieldService.Authorization.Events;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Authorization.Data;

public sealed class AuthorizationDbContext(DbContextOptions<AuthorizationDbContext> options) : DbContext(options)
{
    public DbSet<RoleEvent> RoleEvents => Set<RoleEvent>();
    public DbSet<PermissionEvent> PermissionEvents => Set<PermissionEvent>();
    public DbSet<UserSuspensionEvent> SuspensionEvents => Set<UserSuspensionEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthorizationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
