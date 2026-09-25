using FieldService.Shared.Permissions;
using FieldService.Authorization.Attributes;
using FieldService.SecretKey.Cqrs.Commands.CreateSecretKey;
using FieldService.SecretKey.Cqrs.Commands.DeleteSecretKey;
using FieldService.SecretKey.Cqrs.Queries.GetSecretKeyHistory;
using FieldService.SecretKey.Cqrs.Queries.GetSecretKeyReferenceAt;
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
    
    [HttpGet("secret-key/history")]
    [RequireRoleAttribute(Role.Admin)]
    [RequirePermissionAttribute(SecretKeyPermissions.Read)]
    public async Task<IActionResult> GetSecretKeyHistory(
        [FromQuery] Guid tenantId,
        [FromQuery] string secretName, 
        CancellationToken cancellationToken)
    {
        var userId = ClaimsResolver.GetUserId(HttpContext.User);
        var response = await _mediator.Send(new GetSecretKeyHistoryQuery( tenantId, secretName, userId), cancellationToken);
        
        return Ok(response);
    }
    
    [HttpPost("secret-key/info")]
    [RequirePermissionAttribute(SecretKeyPermissions.Read)]
    public async Task<IActionResult> GetSecretKeyInfo(
        [FromQuery] Guid tenantId,
        [FromQuery] string secretName, 
        [FromQuery] DateTimeOffset at,
        CancellationToken cancellationToken)
    {
        var userId = ClaimsResolver.GetUserId(HttpContext.User);
        var response = await _mediator.Send(new GetSecretKeyReferenceAtQuery( tenantId, secretName, userId, at), cancellationToken);
        
        return Ok(response);
    }
    
    
    
}