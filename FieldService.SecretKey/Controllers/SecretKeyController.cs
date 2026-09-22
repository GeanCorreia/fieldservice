using FieldService.Shared.Permissions;
using FieldService.Authorization.Attributes;
using FieldService.SecretKey.Cqrs.Commands.CreateSecretKey;
using FieldService.SecretKey.Cqrs.Commands.DeleteSecretKey;
using FieldService.SecretKey.Dtos;
using FieldService.Shared.Services;
using FieldService.Shared.Types;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FieldService.SecretKey.Controllers;

[ApiController]
public class SecretKeyController : ControllerBase
{
    private readonly IMediator _mediator;

    public SecretKeyController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPost("secret-key")]
    [RequirePermissionAttribute(SecretKeyPermissions.Write)]
    public async Task<IActionResult> GenerateSecretKey([FromBody]SecretKeyRequestDto request, CancellationToken cancellationToken)
    {
        var userId = ClaimsResolver.GetUserId(HttpContext.User);
        
        await _mediator.Send(new CreateSecretKeyCommand(userId, request.SecretKeyDto), cancellationToken);

        return NoContent();
    }

    [HttpPatch("secret-key")]
    [RequirePermissionAttribute(SecretKeyPermissions.Write)]
    public async Task<IActionResult> UpdateSecretKey([FromBody] SecretKeyRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = ClaimsResolver.GetUserId(HttpContext.User);
        
        return NoContent();
    }

    [HttpDelete("secret-key")]
    [RequireRoleAttribute(Role.Admin)]
    [RequirePermissionAttribute(SecretKeyPermissions.Write)]
    public async Task<IActionResult> DeleteSecretKey(
        [FromQuery] Guid tenantId,
        [FromQuery] string secretName, 
        CancellationToken cancellationToken)
    {
        var userId = ClaimsResolver.GetUserId(HttpContext.User);
        await _mediator.Send(new DeleteSecretKeyCommand(secretName,tenantId, userId ), cancellationToken);
        return NoContent();
    }
    
    
}