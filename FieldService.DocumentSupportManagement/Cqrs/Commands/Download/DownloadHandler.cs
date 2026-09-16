using FieldService.Http.Cqrs.Queries.GetUserTenant;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Types;
using MediatR;

namespace FieldService.DocumentSupportManagement.Cqrs.Commands.Download;

public class DownloadHandler: IRequestHandler<DownloadCommand, StoredFileDownloadResponse>
{
    private readonly IStorageService _storageService;
    private readonly IMediator _mediator;

    public DownloadHandler(IStorageService storageService, IMediator mediator)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }
    public async Task<StoredFileDownloadResponse> Handle(
        DownloadCommand request, 
        CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetUserTenantQuery(request.UserId, request.TenantId), cancellationToken);
        if (user == null)
        {
            throw new UnauthorizedAccessException();
        }
        var downloadRequest = new StoredFileDownloadRequest(request.FileId, user);
        return await _storageService.DownloadAsync(downloadRequest, cancellationToken);
    }
}
