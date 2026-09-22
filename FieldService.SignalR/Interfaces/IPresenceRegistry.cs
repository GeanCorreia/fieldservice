using FieldService.SignalR.Types;

namespace FieldService.SignalR.Interfaces;

public interface IPresenceRegistry
{
    Task RegisterAsync(
        SignalRConnectionContext connectionContext,
        CancellationToken ct = default);
    Task UnregisterConnectionAsync(string connectionId, CancellationToken ct = default);
    Task UnregisterUserAsync(Guid userId, CancellationToken ct = default);
    Task<SignalRConnectionContext?> GetConnectionContextBySessionIdAsync(Guid sessionId, CancellationToken ct = default);
    Task<SignalRConnectionContext?> GetConnectionContextByConnectionIdAsync(string connectionId, CancellationToken ct = default);
    Task <IReadOnlyCollection<SignalRConnectionContext>> GetConnectionContextsByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyCollection<SignalRConnectionContext>> GetConnectionContextsByUserAsync(Guid userId, CancellationToken ct = default);

}