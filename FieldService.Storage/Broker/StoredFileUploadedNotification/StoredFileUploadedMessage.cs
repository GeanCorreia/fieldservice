using System.Diagnostics.CodeAnalysis;
using FieldService.Shared.Message;
using FieldService.Shared.Types;

namespace FieldService.Storage.Broker;

public record StoredFileUploadedMessage : AbstractMessage<StoredFileUploadedPayload>
{
    public static new MessageType MessageType => new MessageType(MessageName, SchemaVersion);

    public static new string MessageName
    {
        get {
            return "storage.stored-file.uploaded";
        }
    }

    public static new SchemaVersion SchemaVersion { get; } = new(1, 0, 0);

    [SetsRequiredMembers]
    public StoredFileUploadedMessage(StoredFileUploadedPayload payload)
        : base(
            payload,
            MessageContext.Create(
                MessageType,
                tenantId: payload.TenantId
            )
        )
    {
        
    }
    
};