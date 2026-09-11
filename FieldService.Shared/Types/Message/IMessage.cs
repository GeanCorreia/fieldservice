using System.Text.Json;
using FieldService.Shared.Types;

namespace FieldService.Shared.Message;

public interface IMessage
{
    static string MessageName { get; }
    static SchemaVersion SchemaVersion { get; }
    IMessagePayload Payload { get; }
    MessageContext Context { get;  }
}

public interface IMessage<TPayload> : IMessage
where TPayload : IMessagePayload
{
    new TPayload Payload { get; }
}