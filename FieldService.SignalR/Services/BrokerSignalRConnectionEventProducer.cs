using FieldService.Broker.Interfaces;
using FieldService.SignalR.Broker.Messages;
using FieldService.SignalR.Interfaces;
using FieldService.SignalR.Types;

namespace FieldService.SignalR.Services;

internal sealed class BrokerSignalRConnectionEventProducer(
    IMessageProducer messageProducer) : ISignalRConnectionEventProducer
{
    public async Task PublishConnectedAsync(SignalRConnectionContext connectionContext, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(connectionContext);
        ct.ThrowIfCancellationRequested();

        var message = new SignalRUserConnectedBrokerMessage(connectionContext);
        await messageProducer.PublishAsync(message.ToBrokerMessage());
    }
}
