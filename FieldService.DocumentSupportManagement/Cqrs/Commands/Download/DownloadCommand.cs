using FieldService.Storage.Types;
using MediatR;

namespace FieldService.DocumentSupportManagement.Cqrs.Commands.Download;

public record DownloadCommand(
    Guid FileId,
    Guid UserId,
    Guid TenantId) : IRequest<StoredFileDownloadResponse>;