using FieldService.Http.Cqrs.Queries.GetUserTenant;
using FieldService.Storage.Dtos;
using FieldService.Storage.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.DocumentSupportManagement.Cqrs.Commands.PresignedUpload;

public class PresignedUploadHandler : IRequestHandler<PresignedUploadCommand, PresignedFileUploadResponseDto>
{
    private readonly IStoragePresignedUrlService _presignedUrlService;
    private readonly IMediator _mediator;
    private readonly ILogger<PresignedUploadHandler> _logger;
    
    public PresignedUploadHandler(
        IStoragePresignedUrlService presignedUrlService,
        IMediator mediator,
        ILogger<PresignedUploadHandler> logger)
    {
        _presignedUrlService = presignedUrlService ?? throw new ArgumentNullException(nameof(presignedUrlService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<PresignedFileUploadResponseDto> Handle(
        PresignedUploadCommand request, 
        CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetUserTenantQuery(request.UserId, request.TenantId), cancellationToken);
        if (user == null)
        {
            throw new UnauthorizedAccessException();
        }
        
        return await _presignedUrlService.CreateUploadUrlAsync(
            request.Request, 
            user, 
            ct: cancellationToken);
    }
}