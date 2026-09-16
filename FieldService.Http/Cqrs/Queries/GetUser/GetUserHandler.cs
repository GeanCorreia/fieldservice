using FieldService.Authentication.Interfaces;
using FieldService.Authorization.Interfaces;
using FieldService.Shared.Types;
using MediatR;


namespace FieldService.Http.Cqrs.Queries.GetUser;

public class GetUserHandler : IRequestHandler<GetUserQuery, UserDto?>
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IAuthenticationService _authenticationService;
    
    public GetUserHandler(
        IAuthorizationService authorizationService,
        IAuthenticationService authenticationService)
    {
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
    }
    
    public async Task<UserDto?> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;

        var userAuthentication = await _authenticationService.GetUserAsync(request.UserId, cancellationToken);
        if (userAuthentication == null)
        {
            return null;
        }
        
        var userAuthorization = await _authorizationService.GetUserAsync(userId, cancellationToken);
        if (userAuthorization == null)
        {
            return null;
        }
        
        var tenantsDto = new List<TenantDto>();
        
        foreach(var tenant in userAuthorization.Tenants)
        {
            var tenantName = userAuthentication.Tenants.FirstOrDefault(t => t.TenantId == tenant.TenantId).Name ?? string.Empty;
            var tenantDto = new TenantDto(
              tenant.TenantId,
              tenantName,
              tenant.Role,
              tenant.Permissions.ToList(),
              tenant.IsActive);

            tenantsDto.Add(tenantDto);
        }
        
        return new UserDto(
            userId,
            tenantsDto);
        
    }
}