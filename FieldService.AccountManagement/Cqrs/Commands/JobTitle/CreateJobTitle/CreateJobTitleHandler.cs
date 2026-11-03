using FieldService.AccountManagement.Entities;
using FieldService.AccountManagement.Interfaces;
using FieldService.Data.Interfaces;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.CreateJobTitle;

internal sealed class CreateJobTitleHandler(
    IAccountManagementRepository repository,
    IAccountManagementService cacheService,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateJobTitleCommand>
{
    public async Task Handle(CreateJobTitleCommand request, CancellationToken cancellationToken)
    {
        
        var hasJobTitle = await repository.HasJobTitleAsync(request.TenantId, request.Title, cancellationToken);
        if (hasJobTitle)
        {
            throw new InvalidOperationException($"A job title with title '{request.Title}' already exists.");
        }
        var jobTitle = JobTitle.Create(
            request.JobTitleId,
            request.TenantId,
            request.Title,
            request.Description,
            request.CreatedBy);

        await repository.SaveAsync(jobTitle, cancellationToken);
        await cacheService.RemoveCache(request.TenantId, entityId: null, cancellationToken);
    }
}
