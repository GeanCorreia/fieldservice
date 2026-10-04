using FieldService.Superset.Configuration;
using FieldService.Superset.Dtos;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using MediatR;
using Microsoft.Extensions.Options;

namespace FieldService.Superset.Cqrs.Queries.GetTenantSupersetResources;

internal class GetTenantSupersetResourcesHandler : IRequestHandler<GetTenantSupersetResourcesQuery, SupersetTenantResources>
{
    private readonly ISupersetTenantService _supersetTenantServices;
    private readonly ISupersetResourceServices _supersetResourceServices;


    public GetTenantSupersetResourcesHandler(
        ISupersetTenantService supersetTenantServices,
        ISupersetResourceServices supersetResourceServices)
    {
        _supersetTenantServices = supersetTenantServices ?? throw new ArgumentNullException(nameof(supersetTenantServices));
        _supersetResourceServices = supersetResourceServices ?? throw new ArgumentNullException(nameof(supersetResourceServices));
    }

    public async Task<SupersetTenantResources> Handle(
        GetTenantSupersetResourcesQuery request, 
        CancellationToken cancellationToken)
    {
        var supersetTenant = await _supersetTenantServices.GetSupersetTenantByIdAsync(request.TenantId, cancellationToken);

        if (supersetTenant == null)
        {
            throw new SupersetTenantNotFoundException(request.TenantId);
        }
        
        if(supersetTenant.Status != SupersetTenantStatus.Active)
        {
            throw new SupersetTenantBlockedException(
                supersetTenant.Id, 
                supersetTenant.Status 
                );
        }
        
        return await _supersetResourceServices.GetSupersetTenantResourcesAsync(
            request.UserId, 
            request.TenantId, 
            cancellationToken);
    }
}