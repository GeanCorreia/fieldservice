using FieldService.SignalR.Dtos;
using FieldService.SignalR.Entities;

namespace FieldService.SignalR.Interfaces;



internal interface IDomainRoomService
{
    Task<IEnumerable<DomainRoom>> GetRoomsAsync(IEnumerable<Guid> roomIds, CancellationToken ct = default);
    Task<IEnumerable<DomainRoom>> GetRoomsForUserAsync(Guid userId, CancellationToken ct = default);
    Task<DomainRoom?> GetRoomAsync(Guid roomId, CancellationToken ct = default);
    Task SaveRoomAsync(DomainRoom room, CancellationToken ct = default);
    Task AddToRoomAsync(DomainRoomRegistryDto dto, CancellationToken ct = default);
    Task AddToRoomAsync(IEnumerable<DomainRoomRegistryDto> entries, CancellationToken ct = default);
    Task RemoveFromRoomAsync(DomainRoomRegistryDto dto, CancellationToken ct = default);
    Task RemoveUserFromRoomAsync(IEnumerable<DomainRoomRegistryDto> entries, CancellationToken ct = default);
    Task UnregisterFromAllRoomsAsync(Guid userId, CancellationToken ct = default);
   
}