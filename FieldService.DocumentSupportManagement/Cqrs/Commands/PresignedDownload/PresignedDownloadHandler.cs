using FieldService.Http.Cqrs.Queries.GetUserTenant;
using FieldService.Storage.Dtos;
using FieldService.Storage.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.DocumentSupportManagement.Cqrs.Commands.PresignedDownload;

public class PresignedDownloadHandler : IRequestHandler<PresignedDownloadCommand, PresignedFileDownloadResponseDto>
{
    private readonly IStoragePresignedUrlService _storagePresignedUrlService;
    private readonly ILogger<PresignedDownloadHandler> _logger;
    private readonly IMediator _mediator;
    
    public PresignedDownloadHandler(
        IStoragePresignedUrlService storagePresignedUrlService, 
        ILogger<PresignedDownloadHandler> logger,
        IMediator mediator)
    {
        _storagePresignedUrlService = storagePresignedUrlService ?? throw new ArgumentNullException(nameof(storagePresignedUrlService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }
    
    public async Task<PresignedFileDownloadResponseDto> Handle(
        PresignedDownloadCommand request, 
        CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetUserTenantQuery(request.UserId, request.TenantId), cancellationToken);
        if (user == null)
        {
            throw new UnauthorizedAccessException();
        }
        return await _storagePresignedUrlService.CreateDownloadUrlAsync(
            request.PresignedFileDownloadRequestDto, user, 
            cancellationToken);
    }
}