using FieldService.Storage.Entities;
using FieldService.Storage.Types;
using MediatR;

namespace FieldService.DocumentSupportManagement.Cqrs.Commands.Upload;

public record UploadCommand(
    Guid UserId,
    Guid TenantId,
    Stream Content,
    string FileName,
    Guid? FileId = null): IRequest<StoredFileUploadResponse> ;