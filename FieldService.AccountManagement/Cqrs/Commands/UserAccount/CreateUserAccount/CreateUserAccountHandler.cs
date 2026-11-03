using FieldService.AccountManagement.Entities;
using FieldService.AccountManagement.Interfaces;
using FieldService.Data.Interfaces;
using FieldService.Shared.Types;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.CreateUserAccount;

internal sealed class CreateUserAccountHandler(
    IAccountManagementRepository repository,
    IAccountManagementService cacheService,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateUserAccountCommand>
{
    public async Task Handle(CreateUserAccountCommand request, CancellationToken cancellationToken)
    {
        if(string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentNullException(nameof(request.Email));
        }
        
        var hasUser = await repository.HasUserAsync(request.TenantId, new Email(request.Email), cancellationToken);
        
        if(hasUser)
        {
            throw new InvalidOperationException($"A user with email '{request.Email}' already exists.");
        }
        
        var teamId = request.TeamId ?? await repository.GetDefaultTeamIdByTenantIdAsync(request.TenantId, cancellationToken);
        var team = await repository.GetTeamByIdAsync(request.TenantId, teamId, cancellationToken)
            ?? throw new KeyNotFoundException($"Team '{teamId}' was not found for tenant '{request.TenantId}'.");
        if (team.IsDeactivated)
            throw new InvalidOperationException($"Team '{teamId}' is deactivated.");

        var jobTitle = await repository.GetJobTitleByIdAsync(request.TenantId, request.JobTitleId, cancellationToken)
            ?? throw new KeyNotFoundException($"JobTitle '{request.JobTitleId}' was not found for tenant '{request.TenantId}'.");
        if (jobTitle.IsDeactivated)
            throw new InvalidOperationException($"JobTitle '{request.JobTitleId}' is deactivated.");

        var userAccount = UserAccount.Create(
                Guid.NewGuid(),
                request.TenantId,
                teamId,
                request.JobTitleId,
                request.FullName,
                request.DisplayName,
                new Email(request.Email),
                new Phone(request.Phone),
                request.CreatedBy)
            ;
        
        await repository.SaveAsync(userAccount, cancellationToken);
        await cacheService.RemoveCache(request.TenantId, entityId: null, cancellationToken);
    }
}
