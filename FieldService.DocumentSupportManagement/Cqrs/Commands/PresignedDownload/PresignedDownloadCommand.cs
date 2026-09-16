using FieldService.Storage.Dtos;
using MediatR;

namespace FieldService.DocumentSupportManagement.Cqrs.Commands.PresignedDownload;

public record PresignedDownloadCommand(
    Guid UserId,
    Guid TenantId,
    PresignedFileDownloadRequestDto  PresignedFileDownloadRequestDto) : IRequest<PresignedFileDownloadResponseDto>;