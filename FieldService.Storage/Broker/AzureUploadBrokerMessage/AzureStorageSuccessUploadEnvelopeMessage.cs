using System.Diagnostics.CodeAnalysis;
using FieldService.Broker.Message;

namespace FieldService.Storage.Broker;

public record AzureStorageSuccessUploadEnvelopeMessage : 
    AbstractBrokerEnvelopeMessage<AzureEventGridBlobCreatedPayload>
{
    public static BrokerPublishContext EnvelopeContext { get; } = new(
        EntityName: AzureStorageSuccessUploadMessage.MessageType.Name,
        TimeToLive: TimeSpan.FromDays(1));

    public BrokerPublishContext PublishContext => EnvelopeContext;

    [SetsRequiredMembers]
    public AzureStorageSuccessUploadEnvelopeMessage(AzureStorageSuccessUploadMessage message)
        : base(message, EnvelopeContext)
    {
        
    }
    
}