using FieldService.Http.Cqrs.Queries.GetUserTenant;
using FieldService.Superset.Attributes;
using FieldService.Superset.Cqrs.Queries.GetSupersetUser;
using FieldService.Superset.Dtos.SupersetApiRequestDto;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.Superset.Cqrs.Commands.CreateUser;

internal class CreateUserHandler : IRequestHandler<CreateUserCommand>
{
    private readonly ILogger<CreateUserHandler> _logger;
    private readonly ISupersetService _supersetService;
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly ISupersetUserManagement _supersetUserManagement;
    private readonly IMediator _mediator;
    
    public CreateUserHandler(
        ILogger<CreateUserHandler> logger,
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

    public async Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetUserTenantQuery(request.UserId, request.TenantId), cancellationToken);
        if (user == null)
        {
            throw new UnauthorizedAccessException();
        }
        if (user.TenantDto.TenantId != request.TenantId)
        {
            throw new UnauthorizedAccessException();
        }
        var tenantConfig = await _supersetService.GetSupersetTenantByTenantIdAsync(request.TenantId, cancellationToken);
        if (tenantConfig == null)
        {
            throw new SupersetTenantNotFoundException(request.TenantId);
        }

        var supersetUser = await _mediator.Send(new GetSupersetUserQuery(request.UserId, request.TenantId), cancellationToken);
        
        if(supersetUser != null)
        {
            throw new InvalidOperationException($"User with ID {request.UserId} already exists in Superset for tenant {request.TenantId}.");
        }
        
        var permissions = request.Permissions
            .Where(p => 
                p.Module == "superset" &&
                p.Resource == "workspace")
            .ToList();

        if (!permissions.Any())
        {
            throw new InvalidOperationException($"User with ID {request.UserId} does not have any valid " +
                                                $"Superset permissions for tenant {request.TenantId}.");
        }
        
        var adminToken = await _supersetAuthService.GetAdminToken(request.TenantId, cancellationToken);
        var bearerToken = $"Bearer {adminToken}";
        
        var host = new Uri(tenantConfig.FqdnUrl);
        
        var roles = SupersetPermissions.MapToSupersetRoles(request.Permissions);
        List<int> roleIds = roles.Select(role => (int)role).ToList();
        
        roleIds.Add((int)SupersetRole.ProductionScope);
        
        var supersetTenantRole = await _supersetAuthService.GetTenantScopeRole(request.TenantId, cancellationToken);
        if (supersetTenantRole == null)
        {
            throw new SupersetTenantNotFoundException(request.TenantId);
        }
        
        roleIds.Add(supersetTenantRole.Id);

        await _supersetUserManagement.CreateUserAsync(
            host,
            bearerToken,
            new SupersetCreateUserApiRequest(
                request.FirstName,
                request.LastName,
                SupersetUsernameResolver.ResolveUsername(request.UserId, request.TenantId),
                request.Email,
                SupersetUsernameResolver.ResolvePassword(request.UserId, request.TenantId),
                true,
                roleIds
                ));
        
        
        

    }
}