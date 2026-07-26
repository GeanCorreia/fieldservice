namespace FieldService.Shared.Types;

public abstract record Message<TPayload, TContext>(
    Guid MessageId,
    Guid TenantId,
    string MessageType,
    DateTime CreatedAt,
    string? CorrelationId,
    FieldService.Shared.Types.Version SchemaVersion,
    TPayload Payload,
    TContext Context);
