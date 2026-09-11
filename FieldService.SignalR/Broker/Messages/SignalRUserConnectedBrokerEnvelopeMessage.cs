using System.Diagnostics.CodeAnalysis;
using FieldService.Broker.Message;
using FieldService.SignalR.Types;

namespace FieldService.SignalR.Broker.Messages;

public sealed record SignalRUserConnectedBrokerEnvelopeMessage
    : AbstractBrokerEnvelopeMessage<SignalRConnectionContext>
{
    public static BrokerPublishContext EnvelopeContext { get; } = new(
        EntityName: UserClientConnectionContextMessage.MessageType.Name,
        TimeToLive: TimeSpan.FromMinutes(5));

    public BrokerPublishContext PublishContext => EnvelopeContext;

    [SetsRequiredMembers]
    public SignalRUserConnectedBrokerEnvelopeMessage(UserClientConnectionContextMessage message)
        : base(message, EnvelopeContext)
    {
        
    }
}