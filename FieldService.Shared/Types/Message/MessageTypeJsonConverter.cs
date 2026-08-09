using System.Text.Json;
using System.Text.Json.Serialization;

namespace FieldService.Shared.Message;

public class MessageTypeJsonConverter : JsonConverter<MessageType>
{
    public override MessageType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(reader.GetString() ?? string.Empty);

    public override void Write(Utf8JsonWriter writer, MessageType value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}