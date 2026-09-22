using FieldService.Data.Interfaces;
using FieldService.SignalR.Entities;
using FieldService.SignalR.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FieldService.SignalR.Data.Repositories;

internal sealed class DomainRoomRepository(
    SignalRDbContext dbContext,
    ISqlUnitOfWork<SignalRDbContext> unitOfWork) : IDomainRoomRepository
{
    public async Task SaveRoomAsync(DomainRoom room, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(room);

        var existing = await dbContext.DomainRooms
            .Include("_events")
            .FirstOrDefaultAsync(x => x.Id == room.Id, ct);

        if (existing is null)
        {
            await dbContext.DomainRooms.AddAsync(room, ct);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(room);
            dbContext.Entry(existing).Property<List<Guid>>("_memberIds").CurrentValue = room.UserMemberIds.ToList();

            var existingEventIds = existing.Events
                .Select(x => x.Id)
                .ToHashSet();

            var newEvents = room.Events
                .Where(x => !existingEventIds.Contains(x.Id))
                .ToList();

            if (newEvents.Count > 0)
                await dbContext.DomainRoomEvents.AddRangeAsync(newEvents, ct);
        }

        await unitOfWork.PersistChangesAsync(ct);
    }

    public async Task<DomainRoom?> GetRoomAsync(Guid roomId, CancellationToken ct = default)
    {
        if (roomId == default)
            throw new ArgumentException("RoomId is required.", nameof(roomId));

        return await dbContext.DomainRooms
            .Include("_events")
            .FirstOrDefaultAsync(x => x.Id == roomId, ct);
    }

    public async Task<IEnumerable<DomainRoom>> GetRoomsAsync(IEnumerable<Guid> roomIds, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(roomIds);

        var roomIdList = roomIds
            .Where(x => x != default)
            .Distinct()
            .ToList();

        if (roomIdList.Count == 0)
            return Enumerable.Empty<DomainRoom>();

        return await dbContext.DomainRooms
            .Include("_events")
            .Where(x => roomIdList.Contains(x.Id))
            .ToListAsync(ct);
    }
}

