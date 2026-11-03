using FieldService.AccountManagement.Interfaces;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.UpdateUserAccountJobTitle;

internal sealed class UpdateUserAccountJobTitleHandler(
    IAccountManagementRepository repository,
    IAccountManagementService cacheService) : IRequestHandler<UpdateUserAccountJobTitleCommand>
{
    public async Task Handle(UpdateUserAccountJobTitleCommand request, CancellationToken cancellationToken)
    {
        var jobTitle = await repository.GetJobTitleByIdAsync(request.TenantId, request.JobTitleId, cancellationToken)
            ?? throw new KeyNotFoundException($"JobTitle '{request.JobTitleId}' was not found for tenant '{request.TenantId}'.");
        if (jobTitle.IsDeactivated)
            throw new InvalidOperationException($"JobTitle '{request.JobTitleId}' is deactivated.");

        var user = await repository.GetUserByIdAsync(request.TenantId, request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User '{request.UserId}' was not found for tenant '{request.TenantId}'.");
        
        user.UpdateJobTitle(request.JobTitleId, request.UpdatedBy);
        await repository.SaveAsync(user, cancellationToken);
        await cacheService.RemoveCache(request.TenantId, entityId: null, cancellationToken);
    }
}
