using FieldService.Shared.Types;
using DomainVersion = FieldService.Shared.Types.Version;

namespace FieldService.Broker.Message;

public sealed record BrokerMessage<TPayload>(
    Guid MessageId,
    Guid TenantId,
    string MessageType,
    DateTime CreatedAt,
    string? CorrelationId,
    DomainVersion SchemaVersion,
    TPayload Payload,
    BrokerPublishContext Context)
    : Message<TPayload, BrokerPublishContext>(
        MessageId,
        TenantId,
        MessageType,
        CreatedAt,
        CorrelationId,
        SchemaVersion,
        Payload,
        Context)
{
    public BrokerPublishContext BrokerPublishContext => Context;
}
