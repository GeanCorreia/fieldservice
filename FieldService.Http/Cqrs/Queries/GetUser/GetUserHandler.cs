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

        var emptyTenantNameUser = await _authorizationService.GetUserAsync(userId, cancellationToken);
        
        if(emptyTenantNameUser == null)
        {
            return null;
        }
        
        var tenantIds = emptyTenantNameUser.Tenants
            .Select(t => t.TenantId)
            .Distinct()
            .ToList();
        
        var tenantReferences = await _mediator
            .Send(new GetTenantReferencesByTenantIdsQuery(tenantIds), cancellationToken);

        var tenantNameById = tenantReferences
            .GroupBy(t => t.Id)
            .ToDictionary(g => g.Key, g => g.First().Name);

        var userWithTenantNames = emptyTenantNameUser.Tenants
            .Select(t => new UserAuthorizationTenantDto(
                t.TenantId,
                tenantNameById.TryGetValue(t.TenantId, out var name) ? name : string.Empty,
                t.Role,
                t.Permissions,
                t.IsActive))
            .ToList();

        return new UserDto(emptyTenantNameUser.Id, userWithTenantNames);
    }
}