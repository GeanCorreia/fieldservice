using System.Diagnostics.CodeAnalysis;
using FieldService.Form.Enums;
using FieldService.Form.Interfaces;
using FieldService.Shared.Message;
using FieldService.Shared.Types;

namespace FieldService.Form.Messages;

public record FormMessagePayload(
    Guid TenantId,
    FormOperation Operation,
    FormInfo FormInfo) : AbstractMessagePayload<FormMessagePayload>;

public record FormMessage: AbstractMessage<FormMessagePayload>
{
    [SetsRequiredMembers]
    public FormMessage(FormMessagePayload payload)
        : base(
            payload,
            MessageContext.Create(
                FormMessage.MessageType,
                tenantId: payload.TenantId))
    {
    }

    public static string MessageName => "FormMessage";
    public static SchemaVersion SchemaVersion => new SchemaVersion(1, 0, 0);
    
}
