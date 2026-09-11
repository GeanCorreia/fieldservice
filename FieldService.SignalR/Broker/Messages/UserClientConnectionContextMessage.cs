using System.Diagnostics.CodeAnalysis;
using FieldService.Shared.Message;
using FieldService.Shared.Types;
using FieldService.SignalR.Types;
using Microsoft.Graph.Models;

namespace FieldService.SignalR.Broker.Messages;

public sealed record UserClientConnectionContextMessage : AbstractMessage<SignalRConnectionContext>
{
    public static new MessageType MessageType => new MessageType(MessageName, SchemaVersion);

    public static string MessageName
    {
        get {
            return "signalr.user-client.ws-connection-context";
        }
    }

    public static new SchemaVersion SchemaVersion { get; } = new(1, 0, 0);
    
    [SetsRequiredMembers]
    public UserClientConnectionContextMessage(SignalRConnectionContext payload)
        : base(payload, MessageContext.Create(
            MessageType,
            payload.SessionId.ToString(),
            payload.TenantId))
    {
    }

    
}