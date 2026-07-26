using FieldService.SignalR.Types;

namespace FieldService.SignalR.Interfaces;

public interface ISignalRConnectionEventProducer
{
    Task PublishConnectedAsync(SignalRConnectionContext connectionContext, CancellationToken ct = default);
}
