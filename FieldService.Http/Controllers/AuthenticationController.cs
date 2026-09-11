using Microsoft.AspNetCore.Mvc;
using FieldService.Http.Cqrs;
using FieldService.Authentication.SessionAttribute;
using FieldService.Http.Cqrs.Commands;
using FieldService.Http.Cqrs.Commands.Login;
using MediatR;

namespace FieldService.Http.Controllers;

public class AuthenticationController : ControllerBase 
{
    private readonly IMediator _mediator;

    public AuthenticationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login/{tenantId}")]
    [SessionAtributes.SessionCreationAttribute]
    public async Task<IActionResult> Login([FromRoute] Guid tenantId, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(tenantId);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _mediator.Send(new LogoutCommand(), cancellationToken);
        return NoContent();
    }

    [HttpGet("tenants")]
    [SessionAtributes.TenantSelectionAttribute]
    public async Task<IActionResult> GetTenants(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserTenantsQuery(), cancellationToken);
        return Ok(result);
    }
}