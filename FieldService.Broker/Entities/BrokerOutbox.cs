using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FieldService.Broker.Interfaces;
using FieldService.Broker.Mappers;
using FieldService.Broker.Message;
using FieldService.Shared.Message;

namespace FieldService.Broker.Entities;

public class BrokerOutbox
{
    public Guid MessageId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? DispatchedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    private JsonDocument _message { get; init; } = JsonDocument.Parse("{}");
    public ServiceBusMessage ServiceBusMessage => JsonSerializer
        .Deserialize<ServiceBusMessage>(_message.RootElement.GetRawText());
    
    
    protected BrokerOutbox() { }

    private BrokerOutbox(
        Guid messageId,
        JsonDocument message,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? dispatchedAt = null,
        DateTimeOffset? expiresAt = null)
    {
        MessageId = messageId;
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        DispatchedAt = dispatchedAt;
        ExpiresAt = expiresAt;
        _message = message;

    }

    public static BrokerOutbox Create(
        IBrokerEnvelopeMessage envelope)
    {
        if (envelope is null)
            throw new ArgumentNullException(nameof(envelope));

        var message = ServiceBusMessageMapper.MapToServiceBusMessage(envelope);
        var brokerOutbox =  new BrokerOutbox(
            messageId: envelope.Message.Context.Id,
            message: JsonDocument.Parse(JsonSerializer.Serialize(message)),
            createdAt: envelope.Message.Context.CreatedAt,
            expiresAt: envelope.PublishContext.TimeToLive.HasValue
                ? envelope.Message.Context.CreatedAt.Add(envelope.PublishContext.TimeToLive.Value)
                : null);
        return brokerOutbox;
    }
        
    public void MarkAsDispatched(DateTimeOffset dispatchedAt)
    {
        DispatchedAt = dispatchedAt;
    }

    public void MarkAsExpired(TimeSpan timeToLive)
    {
        ExpiresAt = DateTimeOffset.UtcNow.Add(timeToLive);
    }

    public void MarkAsExpired(DateTimeOffset expiresAt)
    {
        ExpiresAt = expiresAt;
    }
    
    

    
}