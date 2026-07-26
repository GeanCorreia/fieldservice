namespace FieldService.SignalR.Interfaces;

public interface ISignalRAckProcessor
{
    Task AckDeliveredAsync(Guid notificationId, Guid deviceId, Guid userId, CancellationToken ct = default);
    Task AckReadAsync(Guid notificationId, Guid deviceId, Guid userId, CancellationToken ct = default);
}
