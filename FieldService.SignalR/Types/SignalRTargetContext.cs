using System.Text.Json;
using FieldService.Shared.Types;

namespace FieldService.SignalR.Types;

public enum SignalRTargetType
{
    Device,
    User,
    Room,
    Tenant,
    All
}

public sealed class SignalRTargetContext
{
    public SignalRTargetContext(
        SignalRTargetType targetType,
        string? targetId,
        string messageType,
        SchemaVersion schemaVersion)
    {
        if (targetType == SignalRTargetType.All)
        {
            if (!string.IsNullOrWhiteSpace(targetId))
                throw new ArgumentException("TargetType.All does not accept TargetId.", nameof(targetId));
        }
        else if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("TargetId is required for this TargetType.", nameof(targetId));
        }

        if (string.IsNullOrWhiteSpace(messageType))
            throw new ArgumentException("MessageType is required.", nameof(messageType));

        TargetType = targetType;
        TargetId = targetId;
        MessageType = messageType;
        SchemaVersion = schemaVersion ?? throw new ArgumentNullException(nameof(schemaVersion));
    }

    public SignalRTargetType TargetType { get; private set; }
    public string? TargetId { get; private set; }
    public string MessageType { get; private set; }
    public SchemaVersion SchemaVersion { get; private set; }

    public string ToJson(JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(this, options);
    }

    public static SignalRTargetContext FromJson(string json, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("Json is required.", nameof(json));

        return JsonSerializer.Deserialize<SignalRTargetContext>(json, options)
               ?? throw new InvalidOperationException("SignalRTargetContext could not be deserialized.");
    }
}
