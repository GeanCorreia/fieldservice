using FieldService.Authentication.Entities;
using FieldService.Authentication.Types;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Authentication.Data;

public sealed class AuthenticationDbContext(DbContextOptions<AuthenticationDbContext> options) : DbContext(options)
{
    public DbSet<UserAuthentication> UserAuthentications => Set<UserAuthentication>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionActivity> SessionActivities => Set<SessionActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthenticationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
