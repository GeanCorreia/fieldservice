using FieldService.SignalR.Entities;

namespace FieldService.SignalR.Interfaces;

internal interface IDomainRoomRepository
{
    Task SaveRoomAsync(DomainRoom room, CancellationToken ct = default);
    Task<DomainRoom?> GetRoomAsync(Guid roomId, CancellationToken ct = default);
    Task<IEnumerable<DomainRoom>> GetRoomsAsync(IEnumerable<Guid> roomIds, CancellationToken ct = default);
}