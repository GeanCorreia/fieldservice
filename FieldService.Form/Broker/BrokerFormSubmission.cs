using System.Diagnostics.CodeAnalysis;
using FieldService.Broker.Interfaces;
using FieldService.Broker.Message;
using FieldService.Broker.Producers;
using FieldService.Form.Enums;
using FieldService.Shared.Message;
using FieldService.Shared.Types;

namespace FieldService.Form.Broker;

public record BrokerFormSubmissionPayload(
    Guid TenantId,
    string SlugifyFormTitle,
    Guid SubmissionId,
    FormOperation Operation) : AbstractMessagePayload<BrokerFormSubmissionPayload>;


internal record BrokerFormSubmissionMessage : AbstractMessage<BrokerFormSubmissionPayload>
{
    public static new MessageType MessageType => new MessageType(MessageName, SchemaVersion);

    public static new string MessageName
    {
        get
        {
            return $"form.form-submission";
        }
    }

    public static new SchemaVersion SchemaVersion { get; } = new(1, 0, 0);

    [SetsRequiredMembers]
    public BrokerFormSubmissionMessage(BrokerFormSubmissionPayload payload)
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

internal record BrokerFormSubmissionEnvelopeMessage : AbstractBrokerEnvelopeMessage<BrokerFormSubmissionPayload>
{
    public static BrokerPublishContext EnvelopeContext { get; } = new(
        EntityName: BrokerFormSubmissionMessage.MessageType.Name,
        TimeToLive: TimeSpan.FromDays(1)
    );

    public BrokerPublishContext PublishContext => EnvelopeContext;

    public BrokerFormSubmissionEnvelopeMessage(BrokerFormSubmissionMessage message)
        : base(message, EnvelopeContext)
    {
    }
}

internal class BrokerFormSubmissionBrokerProducer :
    AbstractBrokerProducer<BrokerFormSubmissionEnvelopeMessage, BrokerFormSubmissionPayload>
{
    public static BrokerPublishContext BrokerPublishContext => BrokerFormSubmissionEnvelopeMessage.EnvelopeContext;

    public BrokerFormSubmissionBrokerProducer(
        IBrokerPublisher brokerPublisher
    ) : base(brokerPublisher)
    {
    }

    protected override BrokerFormSubmissionEnvelopeMessage CreateEnvelope(
        BrokerFormSubmissionPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var message = new BrokerFormSubmissionMessage(payload);
        return new BrokerFormSubmissionEnvelopeMessage(message);
    }
}
