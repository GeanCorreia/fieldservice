using FieldService.SignalR.Dtos;
using FieldService.SignalR.Types;

namespace FieldService.SignalR.Interfaces;

internal interface IDomainRoomManager
{
    Task OnConnectedAsync(SignalRConnectionContext context, CancellationToken ct = default);
    Task OnDisconnectedAsync(string connectionId, CancellationToken ct = default);
    Task JoinRoomAsync(DomainRoomRegistryDto entry, CancellationToken ct = default);
    Task JoinRoomAsync(IEnumerable<DomainRoomRegistryDto> entries, CancellationToken ct = default);
    Task LeaveRoomAsync(DomainRoomRegistryDto entry, CancellationToken ct = default);
    Task LeaveRoomAsync(IEnumerable<DomainRoomRegistryDto> entries, CancellationToken ct = default);
    
}