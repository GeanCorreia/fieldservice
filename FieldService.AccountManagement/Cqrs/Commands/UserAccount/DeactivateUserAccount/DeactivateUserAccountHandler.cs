using FieldService.AccountManagement.Data;
using FieldService.AccountManagement.Interfaces;
using FieldService.Data.Interfaces;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.DeactivateUserAccount;

internal sealed class DeactivateUserAccountHandler(
    IAccountManagementRepository repository,
    IAccountManagementService cacheService,
    ISqlUnitOfWork<AccountManagementDbContext> unitOfWork) : IRequestHandler<DeactivateUserAccountCommand>
{
    public async Task Handle(DeactivateUserAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await repository.GetUserByIdAsync(request.TenantId, request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User '{request.UserId}' was not found for tenant '{request.TenantId}'.");

        var teams = (await repository.GetTeamsByLeaderIdAsync(
            request.TenantId, request.UserId, cancellationToken)).ToList();

        if (user.IsDeactivate() && teams.Count == 0)
            return;

        var updatedAt = DateTimeOffset.UtcNow;
        user.Deactivate(request.UpdatedBy, updatedAt);
        foreach (var team in teams)
            team.RemoveLeader(request.UpdatedBy, updatedAt);

        var ownsTransaction = !unitOfWork.HasActiveTransaction;
        if (ownsTransaction)
            await unitOfWork.BeginAsync(cancellationToken);

        try
        {
            await repository.SaveAsync(user, cancellationToken);
            foreach (var team in teams)
                await repository.SaveAsync(team, cancellationToken);

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
