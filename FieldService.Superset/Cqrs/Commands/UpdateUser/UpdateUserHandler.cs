using FieldService.Superset.Attributes;
using FieldService.Superset.Cqrs.Queries.GetSupersetUser;
using FieldService.Superset.Dtos.SupersetApiRequestDto;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.Superset.Cqrs.Commands.UpdateUser;

internal class UpdateUserHandler : IRequestHandler<UpdateUserCommand>
{
    private readonly ISupersetTenantService _supersetTenantService;
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly ISupersetSecurityApi _supersetSecurityApi;
    private readonly IMediator _mediator;
    private readonly ILogger<UpdateUserHandler> _logger;

    public UpdateUserHandler(
        ISupersetTenantService supersetTenantService, 
        ISupersetAuthService supersetAuthService,
        ISupersetSecurityApi supersetSecurityApi, 
        IMediator mediator, 
        ILogger<UpdateUserHandler> logger)
    {
        _supersetTenantService = supersetTenantService ?? throw new ArgumentNullException(nameof(supersetTenantService));
        _supersetAuthService = supersetAuthService ?? throw new ArgumentNullException(nameof(supersetAuthService));
        _supersetSecurityApi = supersetSecurityApi ?? throw new ArgumentNullException(nameof(supersetSecurityApi));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var tenantConfig = await _supersetTenantService.GetSupersetTenantByIdAsync(request.TenantId, cancellationToken);
        if (tenantConfig == null)
        {
            throw new SupersetTenantNotFoundException(request.TenantId);
        }
        
        var supersetUser = await _mediator.Send(new GetSupersetUserQuery(request.UserId, request.TenantId), cancellationToken);

        if (supersetUser == null)
        {
            throw new InvalidOperationException($"User with ID {request.UserId} does not exist in Superset for tenant {request.TenantId}.");
        }

        var updateDto = request.SupersetUserUpdateRequest;
        
        var targetFirstName = !string.IsNullOrWhiteSpace(updateDto.FirstName) ? updateDto.FirstName : supersetUser.FirstName;
        var targetLastName = !string.IsNullOrWhiteSpace(updateDto.LastName) ? updateDto.LastName : supersetUser.LastName;
        var targetEmail = !string.IsNullOrWhiteSpace(updateDto.Email) ? updateDto.Email : supersetUser.Email;
        
        var currentRoleIds = SupersetPermissions.MapToSupersetRoles(supersetUser.Permissions)
            .Select(role => (int)role)
            .ToHashSet();
        
        var permissions = request.SupersetUserUpdateRequest.Permissions;


        if (permissions != null && permissions.Any())
        {
            permissions.Add(SupersetPermissions.ProductionScopePermission);
        }
        
        var permissionsToApply = updateDto.Permissions != null && updateDto.Permissions.Any()
            ? updateDto.Permissions
            : supersetUser.Permissions;

        var targetRoleIdsList = SupersetPermissions.MapToSupersetRoles(permissionsToApply)
            .Select(role => (int)role)
            .ToList();

        var targetRoleIdsSet = targetRoleIdsList.ToHashSet();
        
        var supersetTenantRole = await _supersetAuthService.GetTenantScopeRole(request.TenantId, cancellationToken);
        if (supersetTenantRole == null)
        {
            throw new SupersetTenantNotFoundException(request.TenantId);
        }
        
        targetRoleIdsSet.Add(supersetTenantRole.Id);
        
        bool firstNameChanged = !string.Equals(supersetUser.FirstName, targetFirstName, StringComparison.Ordinal);
        bool lastNameChanged = !string.Equals(supersetUser.LastName, targetLastName, StringComparison.Ordinal);
        bool emailChanged = !string.Equals(supersetUser.Email, targetEmail, StringComparison.OrdinalIgnoreCase);
        bool rolesChanged = !currentRoleIds.SetEquals(targetRoleIdsSet);

        bool hasChanges = firstNameChanged || lastNameChanged || emailChanged || rolesChanged;

        if (!hasChanges)
        {
            return; 
        }
        
        var adminToken = await _supersetAuthService.GetAdminToken(request.TenantId, cancellationToken);
        var bearerToken = $"Bearer {adminToken}";
        var host = new Uri(tenantConfig.FqdnUrl);

        var apiPayload = new SupersetUpdateUserApiRequest(
            FirstName: targetFirstName,
            LastName: targetLastName,
            Email: targetEmail,
            Active: supersetUser.Active,
            RoleIds: targetRoleIdsList
        );

        await _supersetSecurityApi.UpdateUserAsync(
            host,
            bearerToken,
            supersetUser.SupersetUserId,
            apiPayload,
            cancellationToken);
    }
}