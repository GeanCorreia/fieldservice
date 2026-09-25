using Microsoft.AspNetCore.Mvc;
using FieldService.Http.Cqrs;
using FieldService.Authentication.SessionAttribute;
using FieldService.Http.Cqrs.Commands;
using FieldService.Http.Cqrs.Commands.Login;
using FieldService.Http.Cqrs.Queries.GetUser;
using FieldService.Http.Dtos;
using FieldService.Shared.Services;
using MediatR;
using ObservabilityExecutionContext = FieldService.Observability.Services.ExecutionContext;

namespace FieldService.Http.Controllers;

public class AuthenticationController : ControllerBase 
{
    private readonly IMediator _mediator;

    public AuthenticationController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPost("login/{tenantId}")]
    [SessionAtributes.SessionCreationAttribute]
    public async Task<IActionResult> Login([FromRoute] Guid tenantId, CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid();
        ObservabilityExecutionContext.RequestId = requestId;
        ObservabilityExecutionContext.TenantId = tenantId;
        var ipAddress = HttpContext.Connection.RemoteIpAddress!.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
        var principal = HttpContext.User;
        var command = new LoginCommand(requestId, tenantId, principal, ipAddress, userAgent);
        var result = await _mediator.Send(command, cancellationToken);
        
        ObservabilityExecutionContext.SessionId = result;
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var principal = HttpContext.User;
        var sessionId = ClaimsResolver.GetSessionId(principal);
        await _mediator.Send(new LogoutCommand(sessionId), cancellationToken);
        return NoContent();
    }

    [HttpGet("tenants")]
    [SessionAtributes.TenantSelectionAttribute]
    public async Task<IActionResult> GetTenants(CancellationToken cancellationToken)
    {
        var userId = ClaimsResolver.GetUserId(User);
        var query = new GetUserQuery(userId);
        var userDto = await _mediator.Send(query, cancellationToken);
        if (userDto == null)
        {
            return NotFound();
        }
        return Ok(new UserLoginDto(userDto.Tenants));
    }
}