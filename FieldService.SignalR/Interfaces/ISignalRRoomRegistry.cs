namespace FieldService.SignalR.Interfaces;

public interface ISignalRRoomRegistry
{
    Task AddUserToRoomAsync(Guid userId, Guid roomId, CancellationToken ct = default);
    Task RemoveUserFromRoomAsync(Guid userId, Guid roomId, CancellationToken ct = default);
    Task UnregisterFromAllRoomsAsync(string connectionId, CancellationToken ct = default);
    Task RestoreSessionRoomsAsync(string connectionId, Guid sessionId, CancellationToken ct = default);
}