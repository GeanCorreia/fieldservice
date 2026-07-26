using FieldService.SignalR.Types;

namespace FieldService.SignalR.Interfaces;

public interface ISignalRPresenceRegistry
{
    Task RegisterAsync(string connectionId, Guid userId, Guid deviceId, CancellationToken ct = default);
    Task UnregisterAsync(string connectionId, CancellationToken ct = default);
    Task SubscribeTenantAsync(string connectionId, Guid tenantId, CancellationToken ct = default);
    Task UnsubscribeTenantAsync(string connectionId, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyCollection<Guid>> GetUserTenantsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyCollection<string>> GetUserConnectionsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyCollection<SignalRRecipient>> GetUserRecipientsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyCollection<SignalRRecipient>> GetDeviceRecipientsAsync(Guid deviceId, CancellationToken ct = default);
    Task<IReadOnlyCollection<SignalRRecipient>> GetTenantRecipientsAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyCollection<SignalRRecipient>> GetAllRecipientsAsync(CancellationToken ct = default);
}
