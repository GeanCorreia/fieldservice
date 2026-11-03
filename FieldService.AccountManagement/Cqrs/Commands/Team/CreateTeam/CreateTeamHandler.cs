using FieldService.AccountManagement.Entities;
using FieldService.AccountManagement.Interfaces;
using FieldService.Data.Interfaces;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.CreateTeam;

internal sealed class CreateTeamHandler(
    IAccountManagementRepository repository,
    IAccountManagementService cacheService,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateTeamCommand>
{
    public async Task Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        
        var hasTeam = await repository.HasTeamAsync(request.TenantId, request.Name, cancellationToken);
        if(hasTeam)
        {
            throw new InvalidOperationException($"A team with name '{request.Name}' already exists.");
        }
        
        if(request.LeaderId != null)
        {
            var leader = await repository.GetUserByIdAsync(request.TenantId, request.LeaderId.Value, cancellationToken);
            if (leader is null || leader.TenantId != request.TenantId || leader.IsDeactivate())
            {
                throw new InvalidOperationException($"The leader with id '{request.LeaderId}' must be an active user in this tenant.");
            }
        }
        
        var team = Team.Create(
            request.TeamId,
            request.TenantId,
            request.Name,
            request.Description,
            request.LeaderId,
            request.CreatedBy);

        await repository.SaveAsync(team, cancellationToken);
        await cacheService.RemoveCache(request.TenantId, entityId: null, cancellationToken);
    }
}
