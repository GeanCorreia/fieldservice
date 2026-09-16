using System.Text.Json;
using System.Text.Json.Serialization;
using FieldService.Shared.Message;

namespace FieldService.Storage.Broker;

// [JsonConverter(typeof(AzureEventGridBlobCreatedJsonConverter))]
public record AzureEventGridBlobCreatedPayload(
    [property: JsonPropertyName("topic")] string Topic,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("eventType")] string EventType,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("data")] EventGridBlobData Data,
    [property: JsonPropertyName("dataVersion")] string DataVersion,
    [property: JsonPropertyName("metadataVersion")] string MetadataVersion,
    [property: JsonPropertyName("eventTime")] DateTimeOffset EventTime
) : AbstractMessagePayload<AzureEventGridBlobCreatedPayload>
{
    public Guid TenantId => ExtractTenantId();
    
    private Guid ExtractTenantId()
    {
        if (string.IsNullOrWhiteSpace(Subject))
            throw new InvalidOperationException("Subject cannot be null or empty");

        const string tenantsSegment = "/tenants/";
        var tenantsIndex = Subject.IndexOf(tenantsSegment, StringComparison.OrdinalIgnoreCase);

        // Tenta achar com barra inicial "/tenants/" ou sem "tenants/"
        if (tenantsIndex == -1)
        {
            const string tenantsSegmentNoLeadingSlash = "tenants/";
            tenantsIndex = Subject.IndexOf(tenantsSegmentNoLeadingSlash, StringComparison.OrdinalIgnoreCase);

            if (tenantsIndex == -1)
                throw new InvalidOperationException($"Invalid Subject format. Could not find 'tenants/' segment in '{Subject}'.");

            tenantsIndex -= 1; // Ajusta offset para igualar a busca
        }

        var startIndex = tenantsIndex + tenantsSegment.Length;
        var nextSlashIndex = Subject.IndexOf('/', startIndex);

        var tenantIdSpan = nextSlashIndex == -1 
            ? Subject.AsSpan(startIndex) 
            : Subject.AsSpan(startIndex, nextSlashIndex - startIndex);

        if (!Guid.TryParse(tenantIdSpan, out var tenantId))
        {
            throw new FormatException($"The tenantId '{tenantIdSpan.ToString()}' extracted from Subject '{Subject}' is not a valid GUID.");
        }

        return tenantId;
    }
    
    public Guid FileId => ExtractFileId();

    private Guid ExtractFileId()
    {
        var targetPath = !string.IsNullOrWhiteSpace(Subject) 
            ? Subject 
            : Data?.Url;

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            throw new InvalidOperationException("Cannot extract FileId because both 'Subject' and 'Data.Url' are empty.");
        }

        var fileNameSpan = Path.GetFileNameWithoutExtension(targetPath.AsSpan());

        if (!Guid.TryParse(fileNameSpan, out var fileId))
        {
            throw new FormatException($"The file name '{fileNameSpan.ToString()}' in path '{targetPath}' is not a valid GUID.");
        }

        return fileId;
    }
}

// public sealed class AzureEventGridBlobCreatedJsonConverter : JsonConverter<AzureEventGridBlobCreatedPayload>
// {
//     public override AzureEventGridBlobCreatedPayload? Read(
//         ref Utf8JsonReader reader, 
//         Type typeToConvert, 
//         JsonSerializerOptions options)
//     {
//         using var doc = JsonDocument.ParseValue(ref reader);
//         var root = doc.RootElement;
//         
//         if (root.ValueKind == JsonValueKind.Array)
//         {
//             var firstElement = root.EnumerateArray().FirstOrDefault();
//             if (firstElement.ValueKind == JsonValueKind.Undefined)
//                 return null;
//
//             return DeserializeWithoutCustomConverter(firstElement, options);
//         }
//         
//         if (root.ValueKind == JsonValueKind.Object)
//         {
//             return DeserializeWithoutCustomConverter(root, options);
//         }
//
//         return null;
//     }
//
//     public override void Write(
//         Utf8JsonWriter writer, 
//         AzureEventGridBlobCreatedPayload value, 
//         JsonSerializerOptions options)
//     {
//         var copyOptions = CreateCleanOptions(options);
//         
//         JsonSerializer.Serialize(writer, value, copyOptions);
//     }
//
//     private static AzureEventGridBlobCreatedPayload? DeserializeWithoutCustomConverter(
//         JsonElement element, 
//         JsonSerializerOptions options)
//     {
//         var copyOptions = CreateCleanOptions(options);
//         return element.Deserialize<AzureEventGridBlobCreatedPayload>(copyOptions);
//     }
//
//     private static JsonSerializerOptions CreateCleanOptions(JsonSerializerOptions options)
//     {
//         var cleanOptions = new JsonSerializerOptions(options);
//         
//         var existingConverter = cleanOptions.Converters
//             .FirstOrDefault(c => c is AzureEventGridBlobCreatedJsonConverter);
//             
//         if (existingConverter != null)
//         {
//             cleanOptions.Converters.Remove(existingConverter);
//         }
//         
//         cleanOptions.TypeInfoResolver = options.TypeInfoResolver;
//
//         return cleanOptions;
//     }
// }

public record EventGridBlobData(
    [property: JsonPropertyName("api")] string Api,
    [property: JsonPropertyName("clientRequestId")] string ClientRequestId,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("eTag")] string ETag,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("contentLength")] long ContentLength,
    [property: JsonPropertyName("blobType")] string BlobType,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("sequencer")] string Sequencer,
    [property: JsonPropertyName("storageDiagnostics")] EventGridStorageDiagnostics StorageDiagnostics
);

public record EventGridStorageDiagnostics(
    [property: JsonPropertyName("batchId")] string BatchId
);