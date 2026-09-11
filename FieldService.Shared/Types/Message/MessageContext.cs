using System.Text.Json;
using System.Text.Json.Serialization;
using FieldService.Shared.Types;

namespace FieldService.Shared.Message;

public record MessageContext
{
    public Guid Id { get; init; }
    public string CorrelationId { get; init; }
    public Guid? TenantId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public MessageType MessageType { get; init; }
    
    public MessageContext(
        Guid id,
        DateTimeOffset createdAt, 
        MessageType messageType,
        string? correlationId = null,
        Guid? tenantId = null)
    {
        Id = id;
        CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        TenantId = tenantId;
        CreatedAt = createdAt;
        MessageType = messageType;

    }
    
    public static MessageContext Create(
        MessageType messageType,
        string? correlationId = null,
        Guid? tenantId = null)
    {

        return new MessageContext(
            id: Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            messageType,
            correlationId ?? Guid.NewGuid().ToString(),
            tenantId);

    }
}
