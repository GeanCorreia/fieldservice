using FieldService.Http.Cqrs.Queries.GetUserTenant;
using FieldService.Superset.Dtos;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.Superset.Cqrs.Queries.GetTenantSupersetResources;

internal class GetTenantSupersetResourcesHandler : IRequestHandler<GetTenantSupersetResourcesQuery, SupersetTenantResources>
{
    private readonly ISupersetService _supersetService;
    private readonly ILogger<GetTenantSupersetResourcesHandler> _logger;
    private readonly IMediator _mediator;

    public GetTenantSupersetResourcesHandler(
        ISupersetService supersetService, 
        ILogger<GetTenantSupersetResourcesHandler> logger, 
        IMediator mediator)
    {
        _supersetService = supersetService ?? throw new ArgumentNullException(nameof(supersetService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    public async Task<SupersetTenantResources> Handle(
        GetTenantSupersetResourcesQuery request, 
        CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(
            new GetUserTenantQuery(request.UserId, request.TenantId), 
            cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }
        
        if(! await _supersetService.HasSuperset(user.TenantDto.TenantId, cancellationToken))
        {
            throw new SupersetTenantNotFoundException(user.TenantDto.TenantId);
        }
        
        return await _supersetService.GetTenantResourcesAsync(
            user.TenantDto.TenantId, 
            cancellationToken);
    }
}