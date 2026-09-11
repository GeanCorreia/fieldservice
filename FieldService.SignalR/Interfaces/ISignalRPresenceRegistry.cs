using FieldService.SignalR.Types;

namespace FieldService.SignalR.Interfaces;

public interface ISignalRPresenceRegistry
{
    Task RegisterAsync(
        string connectionId,
        Guid userId,
        Guid sessionId,
        Guid tenantId,
        CancellationToken ct = default);
    Task UnregisterAsync(string connectionId, CancellationToken ct = default);
    Task UnregisterFromAllRoomsAsync(string connectionId, CancellationToken ct = default);
    Task<IReadOnlyCollection<string>> GetUserConnectionsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyCollection<SignalRConnectionContext>> GetUserRecipientsAsync(Guid userId, CancellationToken ct = default);
    Task<SignalRConnectionContext?> GetSessionConnectionAsync(Guid sessionId, CancellationToken ct = default);
    Task<IReadOnlyCollection<SignalRConnectionContext>> GetTenantRecipientsAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyCollection<SignalRConnectionContext>> GetAllRecipientsAsync(CancellationToken ct = default);
}