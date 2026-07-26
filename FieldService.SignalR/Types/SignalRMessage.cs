using FieldService.Shared.Types;
using DomainVersion = FieldService.Shared.Types.Version;

namespace FieldService.SignalR.Types;

public sealed record SignalRMessage<TPayload>(
    Guid MessageId,
    Guid TenantId,
    string MessageType,
    DateTime CreatedAt,
    string? CorrelationId,
    DomainVersion SchemaVersion,
    TPayload Payload,
    SignalRTargetContext Context)
    : Message<TPayload, SignalRTargetContext>(
        MessageId,
        TenantId,
        MessageType,
        CreatedAt,
        CorrelationId,
        SchemaVersion,
        Payload,
        Context)
{
    public SignalRTargetContext TargetContext => Context;
}
