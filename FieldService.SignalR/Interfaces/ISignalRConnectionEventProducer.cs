using FieldService.SignalR.Types;

namespace FieldService.SignalR.Interfaces;

public interface ISignalRConnectionEventProducer
{
    Task PublishAsync(SignalRConnectionContext connectionContext, CancellationToken ct = default);
}