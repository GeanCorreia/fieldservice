using FieldService.Broker.Interfaces;
using FieldService.Broker.Message;
using FieldService.Data.Interfaces;
using FieldService.Shared.Message;

namespace FieldService.Broker.Producers;

public abstract class AbstractBrokerProducer<TEnvelope, TPayload> : IBrokerProducer<TPayload>    
    where TEnvelope : IBrokerEnvelopeMessage<TPayload> 
    where TPayload : class, IMessagePayload
{
    private readonly IBrokerPublisher _brokerPublisher;

    private static readonly BrokerPublishContext ResolvedPublishContext = ResolvePublishContext();
    
    public static BrokerPublishContext BrokerPublishContext => ResolvedPublishContext;
    public BrokerPublishContext PublishContext => BrokerPublishContext;

    protected AbstractBrokerProducer(IBrokerPublisher brokerPublisher)
    
    {
        _brokerPublisher = brokerPublisher ?? throw new ArgumentNullException(nameof(brokerPublisher));
    }
    
    
    public async Task PublishAsync(TPayload payload, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(payload, nameof(payload));
        var envelope = CreateEnvelope(payload);

        if (envelope == null)
        {
            throw new InvalidOperationException($"The envelope of type {typeof(TEnvelope).Name} could not be created.");
        }

        await _brokerPublisher.PublishAsync(envelope, ct);

    }

    private static BrokerPublishContext ResolvePublishContext()
    {
        var envelopeType = typeof(TEnvelope);

        var contextProperty = envelopeType.GetProperty("BrokerPublishContext")
            ?? envelopeType.GetProperty("EnvelopeContext");

        if (contextProperty?.GetValue(null) is BrokerPublishContext publishContext)
            return publishContext;

        throw new InvalidOperationException(
            $"Envelope '{envelopeType.FullName}' must expose a public static BrokerPublishContext property named 'BrokerPublishContext' or 'EnvelopeContext'.");
    }

    protected abstract TEnvelope CreateEnvelope(TPayload payload);
} 