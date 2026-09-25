using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FieldService.Broker.Entities;
using FieldService.Broker.Interfaces;
using FieldService.Broker.Message;
using FieldService.Shared.Message;

namespace FieldService.Broker.Mappers;

public static class ServiceBusMessageMapper
{
    public const string TenantIdPropertyName = "TenantId";
    public const string CreatedAtPropertyName = "CreatedAt";
    public const string MessageTypePropertyName = "MessageType";
    

    public static ServiceBusMessage MapToServiceBusMessage(IBrokerEnvelopeMessage envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(
            envelope.Message, 
            envelope.Message.GetType()
        );
        var serviceBusMessage = new ServiceBusMessage(jsonBytes)
        {
            ContentType = "application/json",
            MessageId =envelope.Message.Context.Id.ToString(),
            Subject = envelope.PublishContext.EntityName,
            ScheduledEnqueueTime = envelope.PublishContext.ScheduledEnqueueTimeUtc ?? default,
            CorrelationId = envelope.Message.Context.CorrelationId,
            PartitionKey = envelope.PublishContext.PartitionKey,
            SessionId = envelope.PublishContext.SessionId,
            ReplyTo = envelope.PublishContext.ReplyTo,
        };
        
        if (envelope.PublishContext.TimeToLive is { } timeToLive)
        {
            serviceBusMessage.TimeToLive = timeToLive;
        }
        
        var tenantId = envelope.Message.Context.TenantId?.ToString();
        var createdAt = envelope.Message.Context.CreatedAt.ToString();
        
        if (!string.IsNullOrWhiteSpace(tenantId))
            serviceBusMessage.ApplicationProperties.Add(TenantIdPropertyName, tenantId);
        
        if (!string.IsNullOrWhiteSpace(createdAt))
            serviceBusMessage.ApplicationProperties.Add(CreatedAtPropertyName, createdAt);
        
        serviceBusMessage.ApplicationProperties.Add(MessageTypePropertyName, 
            envelope.Message.Context.MessageType);
        
        
        foreach (var header in envelope.PublishContext.Headers ?? new Dictionary<string, object?>())
        {
            serviceBusMessage.ApplicationProperties.Add(header.Key, header.Value);
        }
        
        return serviceBusMessage;
        
    }
}
