using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FieldService.Cache.Serialization;

internal sealed class MediaTypeHeaderValueJsonConverter : JsonConverter<MediaTypeHeaderValue>
{
    public override MediaTypeHeaderValue Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
                return MediaTypeHeaderValue.Parse("application/octet-stream");

            return MediaTypeHeaderValue.Parse(value);
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;

            if (root.TryGetProperty("MediaType", out var mediaTypeElement))
            {
                var mediaType = mediaTypeElement.GetString();
                if (!string.IsNullOrWhiteSpace(mediaType))
                    return MediaTypeHeaderValue.Parse(mediaType);
            }

            if (root.TryGetProperty("Value", out var valueElement))
            {
                var value = valueElement.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    return MediaTypeHeaderValue.Parse(value);
            }

            return MediaTypeHeaderValue.Parse("application/octet-stream");
        }

        throw new JsonException($"Unsupported token for MediaTypeHeaderValue: {reader.TokenType}");
    }

    public override void Write(
        Utf8JsonWriter writer,
        MediaTypeHeaderValue value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

