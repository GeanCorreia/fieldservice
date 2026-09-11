using FieldService.Broker.Message;
using FieldService.Shared.Message;

namespace FieldService.Broker.Interfaces;

public interface IBrokerPublisher
{
    Task PublishAsync(
        IBrokerEnvelopeMessage brokerEnvelopeEnvelopeMessage, 
        CancellationToken ct = default); 
    
    Task PublishBatchAsync(
        IEnumerable<IBrokerEnvelopeMessage> messages, 
        CancellationToken ct = default);
    

}