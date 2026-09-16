using FieldService.Broker.Interfaces;
using FieldService.Broker.Message;
using FieldService.Broker.Producers;

namespace FieldService.Storage.Broker;

public class StoredFileUploadedMessageProducer : AbstractBrokerProducer<StoredFileUploadedEnvelopeMessage, StoredFileUploadedPayload>
{
    public static BrokerPublishContext BrokerPublishContext => StoredFileUploadedEnvelopeMessage.EnvelopeContext;

    public StoredFileUploadedMessageProducer(
        IBrokerPublisher brokerPublisher) 
        : base(brokerPublisher)
    {
    }

    protected override StoredFileUploadedEnvelopeMessage CreateEnvelope(StoredFileUploadedPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var message = new StoredFileUploadedMessage(payload);
        return new StoredFileUploadedEnvelopeMessage(message);
    }
}