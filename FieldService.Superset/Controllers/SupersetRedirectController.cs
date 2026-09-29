using FieldService.Shared.Services;
using FieldService.Superset.Cqrs.Queries.GetTenantSupersetUsers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FieldService.Superset.Controllers;

public class SupersetRedirectController : ControllerBase
{
    private readonly IMediator _mediator;

    public SupersetRedirectController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("superset-users")]
    public async Task<IActionResult> GetSupersetUser(CancellationToken cancellationToken)
    {
        var user = HttpContext.User;
        var tenantId = ClaimsResolver.GetTenantId(user);
        var result = await _mediator
            .Send(new GetTenantSupersetUsersQuery(tenantId, cancellationToken));
        return Ok(result);
    }
}