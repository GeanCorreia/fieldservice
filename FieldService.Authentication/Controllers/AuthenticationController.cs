using Microsoft.AspNetCore.Mvc;
using FieldService.Authentication.Cqrs;
using FieldService.Authentication.Cqrs.Commands;
using FieldService.Authentication.Cqrs.Commands.Login;
using MediatR;

namespace FieldService.Authentication.Controllers;

public class AuthenticationController : ControllerBase 
{
    private readonly IMediator _mediator;

    public AuthenticationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login/{tenantId}")]
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
    public async Task<IActionResult> GetTenants(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserTenantsQuery(), cancellationToken);
        return Ok(result);
    }
}