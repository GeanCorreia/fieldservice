using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FieldService.Shared.Types;


namespace FieldService.Shared.Message;

public abstract record AbstractMessage<TPayload> : IMessage<TPayload>
    where TPayload : class, IMessagePayload
{
    public static MessageType MessageType => new MessageType(MessageName, SchemaVersion);
    public static string MessageName { get; }
    public static SchemaVersion SchemaVersion { get; }
    public required TPayload Payload { get; init; }
    public MessageContext Context { get; init; }
    
  
    [JsonIgnore]
    IMessagePayload IMessage.Payload => Payload;
    
    protected AbstractMessage() { }
    
    [SetsRequiredMembers]
    protected AbstractMessage(TPayload payload, MessageContext context)
    {
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }
    
}