using FieldService.AccountManagement.Interfaces;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.RemoveTeamLeader;

internal sealed class RemoveTeamLeaderHandler(
    IAccountManagementRepository repository,
    IAccountManagementService cacheService) : IRequestHandler<RemoveTeamLeaderCommand>
{
    public async Task Handle(RemoveTeamLeaderCommand request, CancellationToken cancellationToken)
    {
        var team = await repository.GetTeamByIdAsync(request.TenantId, request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException($"Team '{request.TeamId}' was not found for tenant '{request.TenantId}'.");

        if (!team.LeaderId.HasValue)
            return;

        team.RemoveLeader(request.UpdatedBy, DateTimeOffset.UtcNow);
        await repository.SaveAsync(team, cancellationToken);
        await cacheService.RemoveCache(request.TenantId, entityId: null, cancellationToken);
    }
}
