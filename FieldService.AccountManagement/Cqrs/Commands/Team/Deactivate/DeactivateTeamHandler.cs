using FieldService.AccountManagement.Data;
using FieldService.AccountManagement.Interfaces;
using FieldService.Data.Interfaces;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.DeactivateTeam;

internal sealed class DeactivateTeamHandler(
    IAccountManagementRepository repository,
    IAccountManagementService cacheService,
    ISqlUnitOfWork<AccountManagementDbContext> unitOfWork) : IRequestHandler<DeactivateTeamCommand>
{
    public async Task Handle(DeactivateTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await repository.GetTeamByIdAsync(request.TenantId, request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException($"Team '{request.TeamId}' was not found for tenant '{request.TenantId}'.");

        if (team.IsDeactivated)
            return;

        var destinationId = request.TransferTeam.HasValue &&
            await repository.HasTeamAsync(request.TenantId, request.TransferTeam.Value, cancellationToken)
                ? request.TransferTeam.Value
                : await repository.GetDefaultTeamIdByTenantIdAsync(request.TenantId, cancellationToken);

        if (destinationId == request.TeamId)
            throw new InvalidOperationException("The destination team must differ from the team being deactivated.");

        var destination = await repository.GetTeamByIdAsync(request.TenantId, destinationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Destination team '{destinationId}' was not found for tenant '{request.TenantId}'.");

        if (destination.IsDeactivated)
            throw new InvalidOperationException($"Destination team '{destinationId}' is deactivated.");

        var users = (await repository.GetUsersByTeamIdAsync(
            request.TenantId, request.TeamId, cancellationToken)).ToList();

        var updatedAt = DateTimeOffset.UtcNow;
        foreach (var user in users)
            user.UpdateTeam(destinationId, request.UpdatedBy, updatedAt);

        team.Deactivate(request.UpdatedBy, updatedAt);

        var ownsTransaction = !unitOfWork.HasActiveTransaction;
        if (ownsTransaction)
            await unitOfWork.BeginAsync(cancellationToken);

        try
        {
            foreach (var user in users)
                await repository.SaveAsync(user, cancellationToken);

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
