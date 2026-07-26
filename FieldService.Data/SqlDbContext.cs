using Microsoft.EntityFrameworkCore;

namespace FieldService.Data;

public sealed class SqlDbContext(DbContextOptions<SqlDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
