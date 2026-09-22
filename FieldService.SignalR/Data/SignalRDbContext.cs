using FieldService.SignalR.Entities;
using FieldService.SignalR.Events;
using Microsoft.EntityFrameworkCore;

namespace FieldService.SignalR.Data;

internal class SignalRDbContext(DbContextOptions<SignalRDbContext> options) : DbContext(options)
{
    public DbSet<DomainRoom> DomainRooms => Set<DomainRoom>();
    public DbSet<DomainRoomEvent> DomainRoomEvents => Set<DomainRoomEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SignalRDbContext).Assembly);
    }
    
}