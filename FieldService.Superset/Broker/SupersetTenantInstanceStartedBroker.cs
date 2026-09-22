using System.Diagnostics.CodeAnalysis;
using FieldService.Broker.Interfaces;
using FieldService.Broker.Message;
using FieldService.Broker.Producers;
using FieldService.Shared.Message;
using FieldService.Shared.Types;

namespace FieldService.Superset.Broker;

internal record SupersetTenantInstanceStartedPayload(
    Guid TenantId,
    string FqdnUrl,
    string AzureResourceId
) : AbstractMessagePayload<SupersetTenantInstanceStartedPayload>;

internal record SupersetTenantInstanceStartedMessage : AbstractMessage<SupersetTenantInstanceStartedPayload>
{
    public static new MessageType MessageType => new MessageType(MessageName, SchemaVersion);

    public static new string MessageName
    {
        get {
            return "superset.tenant-instance.started";
        }
    }

    public static new SchemaVersion SchemaVersion { get; } = new(1, 0, 0);

    [SetsRequiredMembers]
    public SupersetTenantInstanceStartedMessage(SupersetTenantInstanceStartedPayload payload)
        : base(
            payload,
            MessageContext.Create(
                MessageType,
                tenantId: payload.TenantId
            )
        )
    {
        
    }
    
};

internal record SupersetTenantInstanceStartedEnvelopeMessage : AbstractBrokerEnvelopeMessage<SupersetTenantInstanceStartedPayload>
{
    public static BrokerPublishContext EnvelopeContext { get; } = new(
        EntityName: SupersetTenantInstanceStartedMessage.MessageType.Name,
        TimeToLive: TimeSpan.FromDays(1));

    public BrokerPublishContext PublishContext => EnvelopeContext;

    public SupersetTenantInstanceStartedEnvelopeMessage(SupersetTenantInstanceStartedMessage message)
        : base(message, EnvelopeContext)
    {
        
    }
    
}

internal class SupersetTenantInstanceStartedBrokerProducer : 
    AbstractBrokerProducer<SupersetTenantInstanceStartedEnvelopeMessage, SupersetTenantInstanceStartedPayload>
{
    public static BrokerPublishContext BrokerPublishContext => SupersetTenantInstanceStartedEnvelopeMessage.EnvelopeContext;
    
    public SupersetTenantInstanceStartedBrokerProducer(
        IBrokerPublisher brokerPublisher) : 
        base(brokerPublisher)
    {
    }

    protected override SupersetTenantInstanceStartedEnvelopeMessage CreateEnvelope(
        SupersetTenantInstanceStartedPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var message = new SupersetTenantInstanceStartedMessage(payload);
        return new SupersetTenantInstanceStartedEnvelopeMessage(message);
    }
}

