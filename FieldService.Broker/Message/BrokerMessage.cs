using DomainVersion = FieldService.Domain.ValueObjects.Version;

namespace FieldService.Broker.Message;

public abstract record BrokerMessage<T>(
    Guid MessageId,
    Guid TenantId,
    string MessageType,
    DateTime OccurredAtUtc,
    string? CorrelationId,
    DomainVersion SchemaVersion,
    T Payload,
    BrokerPublishContext BrokerPublishContext);
