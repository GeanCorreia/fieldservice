using FieldService.AccountManagement.Interfaces;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.UpdateTeam;

internal sealed class UpdateTeamHandler(
    IAccountManagementRepository repository,
    IAccountManagementService cacheService) : IRequestHandler<UpdateTeamCommand>
{
    public async Task Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await repository.GetTeamByIdAsync(request.TenantId, request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException($"Team '{request.TeamId}' was not found for tenant '{request.TenantId}'.");

        var eventsCountBeforeUpdate = team.Events.Count;

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            if (await repository.HasTeamAsync(request.TenantId, request.Name.Trim(), request.TeamId, cancellationToken))
                throw new InvalidOperationException($"A team with name '{request.Name}' already exists.");

            team.UpdateName(request.Name.Trim(), request.UpdatedBy);
        }

        if (request.Description is not null)
            team.UpdateDescription(request.Description.Trim(), request.UpdatedBy);

        if (request.LeaderId.HasValue)
        {
            var leader = await repository.GetUserByIdAsync(request.TenantId, request.LeaderId.Value, cancellationToken);
            if (leader is null || leader.TenantId != request.TenantId || leader.IsDeactivate())
                throw new InvalidOperationException($"The leader with id '{request.LeaderId}' must be an active user in this tenant.");

            team.UpdateLeader(request.LeaderId, request.UpdatedBy);
        }

        if (eventsCountBeforeUpdate == team.Events.Count)
            return;

        await repository.SaveAsync(team, cancellationToken);
        await cacheService.RemoveCache(request.TenantId, entityId: null, cancellationToken);
    }
}
