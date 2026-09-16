using FieldService.Authentication.Interfaces;
using FieldService.Http.Cqrs.Queries.GetUser;
using FieldService.Shared.Types;
using MediatR;

namespace FieldService.Http.Cqrs.Queries.GetUserTenant;

public class GetUserTenantHandler : IRequestHandler<GetUserTenantQuery, UserTenantDto?>
{
    private readonly IMediator _mediator;
    public GetUserTenantHandler(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }
    public async Task<UserTenantDto?> Handle(GetUserTenantQuery request, CancellationToken cancellationToken)
    {
        var query  = new GetUserQuery(request.UserId);
        var user = await _mediator.Send(query, cancellationToken);
        
        if(user == null)
        {
            return null;
        }
        
        var tenant = user.Tenants.FirstOrDefault(t => t.TenantId == request.TenantId);

        if (tenant == null)
        {
            return null;
        }
        
        return new UserTenantDto(
            user.Id,
            tenant);
    }
}
