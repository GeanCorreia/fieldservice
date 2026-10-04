using Amazon.Runtime.Internal;
using FieldService.Superset.Cqrs.Queries.GetSupersetUser;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.Superset.Cqrs.Commands.DeleteUser;

internal class DeleteUserHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly ILogger<DeleteUserHandler> _logger;
    private readonly ISupersetTenantService _supersetTenantService;
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly ISupersetSecurityApi _supersetSecurityApi;
    private readonly IMediator _mediator;
    
    
    public DeleteUserHandler(
        ILogger<DeleteUserHandler> logger,
        ISupersetTenantService supersetTenantService,
        ISupersetAuthService supersetAuthService,
        ISupersetSecurityApi supersetSecurityApi,
        IMediator mediator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supersetTenantService = supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
        _supersetAuthService = supersetAuthService ?? throw new ArgumentNullException(nameof(supersetAuthService));
        _supersetSecurityApi = supersetSecurityApi ?? throw new ArgumentNullException(nameof(supersetSecurityApi));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }
    
    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var supersetTenant = await _supersetTenantService.GetSupersetTenantByIdAsync(request.TenantId, cancellationToken);
        if (supersetTenant == null)
        {
            throw new SupersetTenantNotFoundException(request.TenantId);
        }
        
        var supersetUser = await _mediator.Send(new GetSupersetUserQuery(request.UserId, request.TenantId), cancellationToken);
        
        if(supersetUser == null)
        {
            throw new InvalidOperationException($"User with ID {request.UserId} not exists in Superset for tenant {request.TenantId}.");
        }
        
        var adminToken = await _supersetAuthService.GetAdminToken(request.TenantId, cancellationToken);
        var bearerToken = $"Bearer {adminToken}";
        
        var host = new Uri(supersetTenant.FqdnUrl);
        
        await _supersetSecurityApi.DeleteUserAsync(host, bearerToken, supersetUser.SupersetUserId, cancellationToken);
        
        
    }
}