using FieldService.AccountManagement.Cqrs.Queries.GetTenantReferencesByTenantIds;
using FieldService.Authorization.Interfaces;
using FieldService.Shared.Types;
using MediatR;


namespace FieldService.Http.Cqrs.Queries.GetUser;

public class GetUserHandler : IRequestHandler<GetUserQuery, UserDto?>
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IMediator _mediator;
    
    public GetUserHandler(
        IAuthorizationService authorizationService,
        IMediator mediator)
    {
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }
    
    public async Task<UserDto?> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;

        var user = await _authorizationService.GetUserAsync(userId, cancellationToken);
        
        if (user is null)
            return null;
        
        var tenantIds = user.Tenants
            .Select(t => t.TenantId)
            .Distinct()
            .ToList();
        
        var tenantReferences = await _mediator
            .Send(new GetTenantReferencesByTenantIdsQuery(tenantIds), cancellationToken);

        var tenantNameById = tenantReferences
            .GroupBy(t => t.Id)
            .ToDictionary(g => g.Key, g => g.First().Name);

        var userWithTenantNames = new List<UserAuthorizationTenantDto>();
        foreach (var tenant in user.Tenants)
        {
            if (!tenantNameById.TryGetValue(tenant.TenantId, out var name) ||
                string.IsNullOrWhiteSpace(name))
                continue;

            userWithTenantNames.Add(new UserAuthorizationTenantDto(
                tenant.TenantId,
                name,
                tenant.Role,
                tenant.Permissions,
                tenant.IsActive));
        }
        
        if(userWithTenantNames.Count == 0)
            return null;

        return new UserDto(user.Id, userWithTenantNames);
    }
}