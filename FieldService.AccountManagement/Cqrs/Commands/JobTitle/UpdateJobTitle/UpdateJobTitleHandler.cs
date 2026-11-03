using FieldService.AccountManagement.Interfaces;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.UpdateJobTitle;

internal sealed class UpdateJobTitleHandler(
    IAccountManagementRepository repository,
    IAccountManagementService cacheService) : IRequestHandler<UpdateJobTitleCommand>
{
    public async Task Handle(UpdateJobTitleCommand request, CancellationToken cancellationToken)
    {
        var jobTitle = await repository.GetJobTitleByIdAsync(request.TenantId, request.JobTitleId, cancellationToken)
            ?? throw new KeyNotFoundException($"JobTitle '{request.JobTitleId}' was not found for tenant '{request.TenantId}'.");

        var eventsCountBeforeUpdate = jobTitle.Events.Count;

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            if (await repository.HasJobTitleAsync(request.TenantId, request.Title.Trim(), request.JobTitleId, cancellationToken))
                throw new InvalidOperationException($"A job title with title '{request.Title}' already exists.");

            jobTitle.UpdateTitle(request.Title.Trim(), request.UpdatedBy);
        }

        if (request.Description is not null)
            jobTitle.UpdateDescription(request.Description.Trim(), request.UpdatedBy);

        if (eventsCountBeforeUpdate == jobTitle.Events.Count)
            return;

        await repository.SaveAsync(jobTitle, cancellationToken);
        await cacheService.RemoveCache(request.TenantId, entityId: null, cancellationToken);
    }
}
