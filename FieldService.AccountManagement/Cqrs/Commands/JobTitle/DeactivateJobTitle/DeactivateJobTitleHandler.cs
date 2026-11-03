using FieldService.AccountManagement.Data;
using FieldService.AccountManagement.Interfaces;
using FieldService.Data.Interfaces;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.DeactivateJobTitle;

internal sealed class DeactivateJobTitleHandler(
    IAccountManagementRepository repository,
    IAccountManagementService cacheService,
    ISqlUnitOfWork<AccountManagementDbContext> unitOfWork) : IRequestHandler<DeactivateJobTitleCommand>
{
    public async Task Handle(DeactivateJobTitleCommand request, CancellationToken cancellationToken)
    {
        var jobTitle = await repository.GetJobTitleByIdAsync(request.TenantId, request.JobTitleId, cancellationToken)
            ?? throw new KeyNotFoundException($"JobTitle '{request.JobTitleId}' was not found for tenant '{request.TenantId}'.");

        if (request.TransferJobTitle.HasValue)
        {
            if (request.TransferJobTitle.Value == request.JobTitleId)
                throw new InvalidOperationException("The replacement job title must differ from the job title being deactivated.");

            var replacement = await repository.GetJobTitleByIdAsync(
                request.TenantId, request.TransferJobTitle.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"JobTitle '{request.TransferJobTitle}' was not found for tenant '{request.TenantId}'.");

            if (replacement.TenantId != request.TenantId || replacement.IsDeactivated)
                throw new InvalidOperationException("The replacement job title must be active and belong to this tenant.");
        }

        if (jobTitle.IsDeactivated)
            return;

        var users = (await repository.GetUsersByJobTitleIdAsync(
            request.TenantId, request.JobTitleId, cancellationToken)).ToList();

        if (users.Count > 0 && !request.TransferJobTitle.HasValue)
            throw new InvalidOperationException("A replacement job title is required while users are assigned to this job title.");

        var updatedAt = DateTimeOffset.UtcNow;
        foreach (var user in users)
            user.UpdateJobTitle(request.TransferJobTitle!.Value, request.UpdatedBy, updatedAt);

        jobTitle.Deactivate(request.UpdatedBy, updatedAt);

        var ownsTransaction = !unitOfWork.HasActiveTransaction;
        if (ownsTransaction)
            await unitOfWork.BeginAsync(cancellationToken);

        try
        {
            foreach (var user in users)
                await repository.SaveAsync(user, cancellationToken);

            await repository.SaveAsync(jobTitle, cancellationToken);
            unitOfWork.OnCommitted(ct => cacheService.RemoveCache(request.TenantId, entityId: null, ct));
            if (ownsTransaction)
                await unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            if (ownsTransaction && unitOfWork.HasActiveTransaction)
                await unitOfWork.RollbackAsync(CancellationToken.None);

            throw;
        }
    }
}
