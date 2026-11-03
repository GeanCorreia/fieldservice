using FieldService.AccountManagement.Interfaces;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.UserAccountTeamTransfer;

internal sealed class UserAccountTeamTransferHandler(
    IAccountManagementRepository repository,
    IAccountManagementService cacheService) : IRequestHandler<UserAccountTeamTransferCommand>
{
    public async Task Handle(UserAccountTeamTransferCommand request, CancellationToken cancellationToken)
    {
        var team = await repository.GetTeamByIdAsync(request.TenantId, request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException($"Team '{request.TeamId}' was not found for tenant '{request.TenantId}'.");
        if (team.IsDeactivated)
            throw new InvalidOperationException($"Team '{request.TeamId}' is deactivated.");

        var user = await repository.GetUserByIdAsync(request.TenantId, request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User '{request.UserId}' was not found for tenant '{request.TenantId}'.");

        var eventsCountBeforeUpdate = user.Events.Count;
        user.UpdateTeam(request.TeamId, request.UpdatedBy);

        if (eventsCountBeforeUpdate == user.Events.Count)
            return;

        await repository.SaveAsync(user, cancellationToken);
        await cacheService.RemoveCache(request.TenantId, entityId: null, cancellationToken);
    }
}
