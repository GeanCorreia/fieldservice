using System.Diagnostics.CodeAnalysis;
using FieldService.Broker.Interfaces;
using FieldService.Broker.Message;
using FieldService.Broker.Producers;
using FieldService.Shared.Message;
using FieldService.Shared.Types;

namespace FieldService.Superset.Broker;

internal record SupersetTenantDeploymentPayload(
    Guid TenantId,
    string FqdnUrl,
    string AzureResourceId
) : AbstractMessagePayload<SupersetTenantDeploymentPayload>;

internal record SupersetTenantDeploymentMessage : AbstractMessage<SupersetTenantDeploymentPayload>
{
    public static new MessageType MessageType => new MessageType(MessageName, SchemaVersion);

    public static new string MessageName
    {
        get
        {
            return "superset.tenant-deployment.created";
        }
    }

    public static new SchemaVersion SchemaVersion { get; } = new(1, 0, 0);

    [SetsRequiredMembers]
    public SupersetTenantDeploymentMessage(SupersetTenantDeploymentPayload payload)
        : base(
            payload,
            MessageContext.Create(
                MessageType,
                tenantId: payload.TenantId
            )
        )
    {
    }
}

internal record SupersetTenantDeploymentEnvelopeMessage : AbstractBrokerEnvelopeMessage<SupersetTenantDeploymentPayload>
{
    public static BrokerPublishContext EnvelopeContext { get; } = new(
        EntityName: SupersetTenantDeploymentMessage.MessageType.Name,
        TimeToLive: TimeSpan.FromDays(1)
    );

    public BrokerPublishContext PublishContext => EnvelopeContext;

    public SupersetTenantDeploymentEnvelopeMessage(SupersetTenantDeploymentMessage message)
        : base(message, EnvelopeContext)
    {
    }
}

internal class SupersetTenantDeploymentBrokerProducer :
    AbstractBrokerProducer<SupersetTenantDeploymentEnvelopeMessage, SupersetTenantDeploymentPayload>
{
    public static BrokerPublishContext BrokerPublishContext => SupersetTenantDeploymentEnvelopeMessage.EnvelopeContext;

    public SupersetTenantDeploymentBrokerProducer(
        IBrokerPublisher brokerPublisher
    ) : base(brokerPublisher)
    {
    }

    protected override SupersetTenantDeploymentEnvelopeMessage CreateEnvelope(
        SupersetTenantDeploymentPayload payload
    )
    {
        ArgumentNullException.ThrowIfNull(payload);
        var message = new SupersetTenantDeploymentMessage(payload);
        return new SupersetTenantDeploymentEnvelopeMessage(message);
    }
}

