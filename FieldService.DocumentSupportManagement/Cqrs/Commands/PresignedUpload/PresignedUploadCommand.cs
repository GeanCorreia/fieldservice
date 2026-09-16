using FieldService.Storage.Dtos;
using MediatR;

namespace FieldService.DocumentSupportManagement.Cqrs.Commands.PresignedUpload;

public record PresignedUploadCommand(
    Guid UserId,
    Guid TenantId,
    PresignedFileUploadRequestDto Request) : IRequest<PresignedFileUploadResponseDto>;
    