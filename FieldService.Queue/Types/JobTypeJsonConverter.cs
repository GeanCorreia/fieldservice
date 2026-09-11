using System.Text.Json;
using System.Text.Json.Serialization;

namespace FieldService.Queue.Types;

public class JobTypeJsonConverter : JsonConverter<JobType>
{
    public override JobType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(reader.GetString() ?? string.Empty);

    public override void Write(Utf8JsonWriter writer, JobType value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}
