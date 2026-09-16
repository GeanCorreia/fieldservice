using System.ComponentModel;
using System.Net.Http.Headers;
using FieldService.Shared.Message;

namespace FieldService.Storage.Broker;

public record StoredFileUploadedPayload(
    string FileCategory,
    Guid FileId,
    Guid TenantId,
    string FileName,
    MediaTypeHeaderValue ContentType
    ) : AbstractMessagePayload<StoredFileUploadedPayload>;
