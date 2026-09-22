using FieldService.SecretKey.Events;
using Microsoft.EntityFrameworkCore;

namespace FieldService.SecretKey.Data;

internal class SecretKeyDbContext : DbContext
{
    public DbSet<Entities.SecretKey> SecretKeys => Set<Entities.SecretKey>();
    public DbSet<SecretKeyEvent> SecretKeyEvents => Set<SecretKeyEvent>();
}