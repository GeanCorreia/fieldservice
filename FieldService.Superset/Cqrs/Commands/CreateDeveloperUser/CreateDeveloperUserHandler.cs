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
    private readonly ISupersetTenantService _supersetTenantService;
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly ISupersetSecurityApi _supersetSecurityApi;
    private readonly IMediator _mediator;
    private readonly SupersetRevokeTemporaryDeveloperUserProducer _revokeTemporaryDeveloperUserProducer;

    public CreateDeveloperUserHandler(ILogger<CreateDeveloperUserHandler> logger, ISupersetTenantService supersetTenantService,
        ISupersetAuthService supersetAuthService, ISupersetSecurityApi supersetSecurityApi, IMediator mediator,
        SupersetRevokeTemporaryDeveloperUserProducer revokeTemporaryDeveloperUserProducer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supersetTenantService = supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
        _supersetAuthService = supersetAuthService ?? throw new ArgumentNullException(nameof(supersetAuthService));
        _supersetSecurityApi =
            supersetSecurityApi ?? throw new ArgumentNullException(nameof(supersetSecurityApi));
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
        var tenantConfig = await _supersetTenantService.GetSupersetTenantByIdAsync(request.TenantId, cancellationToken);
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
        SupersetTenant tenant,
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
        
        var host = new Uri(tenant.FqdnUrl);
        var roles = SupersetPermissions.MapToSupersetRoles(permissions);
        var supersetTenantRole = await _supersetAuthService.GetTenantScopeRole(request.TenantId, cancellationToken);
        if (supersetTenantRole == null)
        {
            throw new SupersetTenantNotFoundException(request.TenantId);
        }
        
        List<int> roleIds = roles.Select(role => (int)role).ToList();
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