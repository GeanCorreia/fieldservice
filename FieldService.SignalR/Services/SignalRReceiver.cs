using FieldService.SignalR.Interfaces;

namespace FieldService.SignalR.Services;

internal sealed class SignalRReceiver(ISignalRAckProcessor ackProcessor) : ISignalRReceiver
{
    public Task AckDeliveredAsync(Guid notificationId, Guid deviceId, Guid userId, CancellationToken ct = default)
    {
        return ackProcessor.AckDeliveredAsync(notificationId, deviceId, userId, ct);
    }

    public Task AckReadAsync(Guid notificationId, Guid deviceId, Guid userId, CancellationToken ct = default)
    {
        return ackProcessor.AckReadAsync(notificationId, deviceId, userId, ct);
    }
}
