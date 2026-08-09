using FieldService.Shared.Types;

namespace FieldService.Shared.Message;

using System.Text.Json.Serialization;

[JsonConverter(typeof(MessageTypeJsonConverter))]
public readonly record struct MessageType
{
    public string Value { get; }
    public MessageType(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }
    
    public static MessageType From<TPayload>() => new(typeof(TPayload).Name);
    public static MessageType From(Type type) => new(type.Name);

    public static implicit operator string(MessageType messageType) => messageType.Value;
    public static implicit operator MessageType(string value) => new(value);

    public override string ToString() => Value;
}

public record MessageContext(
    Guid Id,
    string TraceId,
    Guid TenantId,
    MessageType MessageType
);


public record Message<TPayload>(
    SchemaVersion SchemaVersion,
    TPayload Payload,
    MessageContext Context);
