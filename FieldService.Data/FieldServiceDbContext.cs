using Microsoft.EntityFrameworkCore;

namespace FieldService.Data;

public sealed class FieldServiceDbContext(DbContextOptions<FieldServiceDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
