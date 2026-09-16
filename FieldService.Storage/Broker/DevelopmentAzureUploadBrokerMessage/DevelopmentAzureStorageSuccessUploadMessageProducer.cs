using FieldService.Broker.Interfaces;
using FieldService.Broker.Message;
using FieldService.Broker.Producers;

namespace FieldService.Storage.Broker.DevelopmentAzureUploadBrokerMessage;

internal class DevelopmentAzureStorageSuccessUploadMessageProducer : AbstractBrokerProducer<AzureStorageSuccessUploadEnvelopeMessage, AzureEventGridBlobCreatedPayload>
{
    public static BrokerPublishContext BrokerPublishContext => StoredFileUploadedEnvelopeMessage.EnvelopeContext;
    
    public DevelopmentAzureStorageSuccessUploadMessageProducer(
        IBrokerPublisher brokerPublisher
        ) : base(brokerPublisher)
    {
    }
    
    protected override AzureStorageSuccessUploadEnvelopeMessage CreateEnvelope(AzureEventGridBlobCreatedPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var message = new AzureStorageSuccessUploadMessage(payload);
        return new AzureStorageSuccessUploadEnvelopeMessage(message);
    }
}