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
    private readonly ISupersetService _supersetService;
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly ISupersetUserManagement _supersetUserManagement;
    private readonly IMediator _mediator;
    
    
    public DeleteUserHandler(
        ILogger<DeleteUserHandler> logger,
        ISupersetService supersetService,
        ISupersetAuthService supersetAuthService,
        ISupersetUserManagement supersetUserManagement,
        IMediator mediator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supersetService = supersetService ?? throw new ArgumentNullException(nameof(supersetService));
        _supersetAuthService = supersetAuthService ?? throw new ArgumentNullException(nameof(supersetAuthService));
        _supersetUserManagement = supersetUserManagement ?? throw new ArgumentNullException(nameof(supersetUserManagement));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }
    
    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var tenantConfig = await _supersetService.GetSupersetTenantByTenantIdAsync(request.TenantId, cancellationToken);
        if (tenantConfig == null)
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
        
        var host = new Uri(tenantConfig.FqdnUrl);
        
        await _supersetUserManagement.DeleteUserAsync(host, bearerToken, supersetUser.SupersetUserId, cancellationToken);
        
        
    }
}