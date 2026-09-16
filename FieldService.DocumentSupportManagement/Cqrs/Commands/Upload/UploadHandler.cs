using FieldService.Data.Interfaces;
using FieldService.Http.Cqrs.Queries.GetUserTenant;
using FieldService.Storage.Data;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Types;
using MediatR;

namespace FieldService.DocumentSupportManagement.Cqrs.Commands.Upload;

public class UploadHandler : IRequestHandler<UploadCommand, StoredFileUploadResponse>
{
    private readonly IStoredFileService _storedFileService;
    private readonly IMediator _mediator;
    private readonly ISqlUnitOfWork<StorageDbContext> _unitOfWork;
    private readonly IStorageService _storageService;
    
    public UploadHandler(
        IStoredFileService storedFileService,
        IMediator mediator, 
        ISqlUnitOfWork<StorageDbContext> unitOfWork, 
        IStorageService storageService)
    {
        _storedFileService = storedFileService ?? throw new ArgumentNullException(nameof(storedFileService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
    }


    public async Task<StoredFileUploadResponse> Handle(
        UploadCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetUserTenantQuery(request.UserId, request.TenantId), cancellationToken);
        if (user == null)
        {
            throw new UnauthorizedAccessException();
        }

        var category = await _storedFileService.GetCategoryByIdAsync(
            TestStoredFileCategory.CategoryId,
            user,
            cancellationToken);

        if (category == null)
        {
            throw new InvalidOperationException($"File category not found.");
        }
        
        var uploadRequest = new StoredFileUploadRequest(
            request.Content,
            category,
            user,
            request.FileName,
            request.FileId);

        await _unitOfWork.BeginAsync(cancellationToken);
        try
        {
            var uploadResponse = await _storageService.UploadAsync(uploadRequest, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            return uploadResponse;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}