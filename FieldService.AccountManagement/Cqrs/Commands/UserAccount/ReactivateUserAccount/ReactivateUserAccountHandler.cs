using FieldService.AccountManagement.Data;
using FieldService.AccountManagement.Interfaces;
using FieldService.Data.Interfaces;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.ReactivateUserAccount;

internal sealed class ReactivateUserAccountHandler(
    IAccountManagementRepository repository,
    IAccountManagementService cacheService,
    ISqlUnitOfWork<AccountManagementDbContext> unitOfWork) : IRequestHandler<ReactivateUserAccountCommand>
{
    public async Task Handle(ReactivateUserAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await repository.GetUserByIdAsync(request.TenantId, request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User '{request.UserId}' was not found for tenant '{request.TenantId}'.");

        if (!user.IsDeactivate())
            return;

        user.Reactivate(request.UpdatedBy, DateTimeOffset.UtcNow);

        var ownsTransaction = !unitOfWork.HasActiveTransaction;
        if (ownsTransaction)
            await unitOfWork.BeginAsync(cancellationToken);

        try
        {
            await repository.SaveAsync(user, cancellationToken);
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
