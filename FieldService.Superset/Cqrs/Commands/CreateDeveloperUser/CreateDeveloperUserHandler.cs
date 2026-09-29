using FieldService.Http.Cqrs.Queries.GetUserTenant;
using FieldService.Queue.Types;
using FieldService.Shared.Types;
using FieldService.Superset.Attributes;
using FieldService.Superset.Cqrs.Commands.UpdateUser;
using FieldService.Superset.Cqrs.Queries.GetSupersetUser;
using FieldService.Superset.Dtos;
using FieldService.Superset.Dtos.SupersetApiRequestDto;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Jobs;
using FieldService.Superset.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.Superset.Cqrs.Commands.CreateDeveloperUser;

internal class CreateDeveloperUserHandler : IRequestHandler<CreateDeveloperUserCommand>
{
    
    private readonly ILogger<CreateDeveloperUserHandler> _logger;
    private readonly ISupersetService _supersetService;
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly ISupersetUserManagement _supersetUserManagement;
    private readonly IMediator _mediator;
    private readonly SupersetRevokeTemporaryDeveloperUserProducer _revokeTemporaryDeveloperUserProducer;

    public CreateDeveloperUserHandler(ILogger<CreateDeveloperUserHandler> logger, ISupersetService supersetService,
        ISupersetAuthService supersetAuthService, ISupersetUserManagement supersetUserManagement, IMediator mediator,
        SupersetRevokeTemporaryDeveloperUserProducer revokeTemporaryDeveloperUserProducer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supersetService = supersetService ?? throw new ArgumentNullException(nameof(supersetService));
        _supersetAuthService = supersetAuthService ?? throw new ArgumentNullException(nameof(supersetAuthService));
        _supersetUserManagement =
            supersetUserManagement ?? throw new ArgumentNullException(nameof(supersetUserManagement));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _revokeTemporaryDeveloperUserProducer = revokeTemporaryDeveloperUserProducer ??
                                                throw new ArgumentNullException(
                                                    nameof(revokeTemporaryDeveloperUserProducer));
    }

    public async Task Handle(
        CreateDeveloperUserCommand request, 
        CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetUserTenantQuery(
            request.UserId, 
            request.TenantId), cancellationToken);
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

        if (supersetUser != null)
        {
            await HandleExistingUser(
                request, 
                supersetUser, 
                cancellationToken);
            
        }
        else
        {
            await HandleNewUser(
                tenantConfig, 
                request, 
                cancellationToken);
        }
        HandleAccessDuration(request);
    }
    
    private void HandleAccessDuration(
        CreateDeveloperUserCommand request)
    {
        if(!request.AccessDuration.HasValue)
        {
            return;
        }
        
        var payload = new SupersetRevokeTemporaryDeveloperUserPayload(request.UserId, request.TenantId);
        _revokeTemporaryDeveloperUserProducer.PublishDelayed(payload, request.AccessDuration.Value);
    }

    private async Task HandleExistingUser(
        CreateDeveloperUserCommand request,
        SupersetUserDetailDto supersetUser,
        CancellationToken cancellationToken)
    {
        if(supersetUser.Permissions.Contains(SupersetPermissions.DevelopmentScopePermission))  
        {
            throw new InvalidOperationException($"User with ID {request.UserId} already has Developer permissions.");
        }
            
        supersetUser.Permissions.Add(SupersetPermissions.DevelopmentScopePermission);
        var updateRequest = new SupersetUserUpdateRequest(Permissions: supersetUser.Permissions);
        await _mediator.Send(new UpdateUserCommand(request.UserId, request.TenantId, updateRequest), cancellationToken);
    }

    private async Task HandleNewUser(
        SupersetTenantConfig tenantConfig,
        CreateDeveloperUserCommand request, 
        CancellationToken cancellationToken)
    {
        var permissions = new List<Permission>
        {
            SupersetPermissions.AccessPermission,
            SupersetPermissions.DevelopmentScopePermission,
            SupersetPermissions.AlphaPermission,
            SupersetPermissions.SqlLabPermission
        };
        
        var adminToken = await _supersetAuthService.GetAdminToken(request.TenantId, cancellationToken);
        var bearerToken = $"Bearer {adminToken}";
        
        var host = new Uri(tenantConfig.FqdnUrl);
        var roles = SupersetPermissions.MapToSupersetRoles(permissions);
        var supersetTenantRole = await _supersetAuthService.GetTenantScopeRole(request.TenantId, cancellationToken);
        if (supersetTenantRole == null)
        {
            throw new SupersetTenantNotFoundException(request.TenantId);
        }
        
        List<int> roleIds = roles.Select(role => (int)role).ToList();
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