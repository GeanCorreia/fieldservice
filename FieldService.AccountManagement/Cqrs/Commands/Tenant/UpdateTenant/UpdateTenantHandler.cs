using FieldService.AccountManagement.Interfaces;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.UpdateTenant;

internal sealed class UpdateTenantHandler(
    IAccountManagementRepository repository,
    IAccountManagementService cacheService) : IRequestHandler<UpdateTenantCommand>
{
    public async Task Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await repository.GetTenantByIdAsync(request.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException($"Tenant '{request.TenantId}' was not found.");

        var eventsCountBeforeUpdate = tenant.Events.Count;

        if (!string.IsNullOrWhiteSpace(request.Name))
            tenant.UpdateName(request.Name.Trim(), request.UpdatedBy);

        if (eventsCountBeforeUpdate == tenant.Events.Count)
            return;

        await repository.SaveAsync(tenant, cancellationToken);
        await cacheService.RemoveCache(request.TenantId, entityId: null, cancellationToken);
    }
}
