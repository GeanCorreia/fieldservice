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
    private readonly ISupersetTenantService _supersetTenantService;
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly ISupersetSecurityApi _supersetSecurityApi;
    private readonly IMediator _mediator;
    
    public CreateUserHandler(
        ILogger<CreateUserHandler> logger,
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
        var tenantConfig = await _supersetTenantService.GetSupersetTenantByIdAsync(request.TenantId, cancellationToken);
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
        
        roleIds.Add((int)SupersetRoleType.ProductionScope);
        
        var supersetTenantRole = await _supersetAuthService.GetTenantScopeRole(request.TenantId, cancellationToken);
        if (supersetTenantRole == null)
        {
            throw new SupersetTenantNotFoundException(request.TenantId);
        }
        
        roleIds.Add(supersetTenantRole.Id);

        await _supersetSecurityApi.CreateUserAsync(
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