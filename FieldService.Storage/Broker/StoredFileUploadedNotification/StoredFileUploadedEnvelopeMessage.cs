using FieldService.Broker.Message;

namespace FieldService.Storage.Broker;

public record StoredFileUploadedEnvelopeMessage : AbstractBrokerEnvelopeMessage<StoredFileUploadedPayload>
{
    public static BrokerPublishContext EnvelopeContext { get; } = new(
        EntityName: StoredFileUploadedMessage.MessageType.Name,
        TimeToLive: TimeSpan.FromDays(1));

    public BrokerPublishContext PublishContext => EnvelopeContext;

    public StoredFileUploadedEnvelopeMessage(StoredFileUploadedMessage message)
        : base(message, EnvelopeContext)
    {
        
    }
    
}