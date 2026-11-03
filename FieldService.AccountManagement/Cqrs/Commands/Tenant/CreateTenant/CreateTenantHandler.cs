using FieldService.AccountManagement.Entities;
using FieldService.AccountManagement.Interfaces;
using FieldService.Authorization.Interfaces;
using FieldService.Data.Interfaces;
using FieldService.Shared.Configuration;
using MediatR;
using Microsoft.Extensions.Options;

namespace FieldService.AccountManagement.Cqrs.Commands.CreateTenant;

internal sealed class CreateTenantHandler(
    IOptions<ApplicationAccountOptions> options,
    IAuthorizationService authorizationService,
    IAccountManagementRepository repository,
    IAccountManagementService cacheService,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateTenantCommand, Guid>
{
    public async Task<Guid> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var createdBy = await authorizationService.GetUserAsync(request.CreatedBy, cancellationToken);

        if (createdBy is null || createdBy.Tenants.All(t => t.TenantId != options.Value.TenantId))
            throw new UnauthorizedAccessException();

        var tenant = Tenant.Create(Guid.NewGuid(), request.Name, request.CreatedBy);

        await repository.SaveAsync(tenant, cancellationToken);
        await cacheService.RemoveCache(tenant.TenantId, entityId: null, cancellationToken);

        return tenant.TenantId;
    }
}
