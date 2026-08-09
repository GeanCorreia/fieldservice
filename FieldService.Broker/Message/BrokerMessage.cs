using FieldService.Shared.Types;

namespace FieldService.Broker.Message;

public sealed record BrokerMessage<TPayload>(
    Guid MessageId,
    Guid TenantId,
    string MessageType,
    DateTime CreatedAt,
    string? TraceId,
    SchemaVersion SchemaVersion,
    TPayload Payload,
    BrokerPublishContext Context)
    : Message<TPayload, BrokerPublishContext>(
        MessageId,
        TenantId,
        MessageType,
        CreatedAt,
        TraceId,
        SchemaVersion,
        Payload,
        Context)
{
    public BrokerPublishContext BrokerPublishContext => Context;
}
