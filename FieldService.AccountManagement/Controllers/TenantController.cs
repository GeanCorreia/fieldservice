using FieldService.AccountManagement.Cqrs.Queries.GetTenantById;
using FieldService.AccountManagement.Cqrs.Queries.GetTenantReferenceById;
using FieldService.AccountManagement.Cqrs.Commands.UpdateTenant;
using FieldService.AccountManagement.Dtos.Request;
using FieldService.AccountManagement.Dtos.Response;
using FieldService.Shared.Services;
using FieldService.Authorization.Attributes;
using Permissions = FieldService.AccountManagement.Attributes.Permissions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FieldService.AccountManagement.Controllers;

[ApiController]
public sealed class TenantController(IMediator mediator) : ControllerBase
{
    [HttpGet("account-management/tenant")]
    [RequirePermission(Permissions.Read)]
    public async Task<IActionResult> GetTenant(CancellationToken cancellationToken)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);
        var tenant = await mediator.Send(new GetTenantByIdQuery(tenantId), cancellationToken);
        return tenant is null ? NotFound() : Ok(tenant.ToResponse());
    }

    [HttpGet("account-management/tenant/reference")]
    [RequirePermission(Permissions.Read)]
    public async Task<IActionResult> GetTenantReference(CancellationToken cancellationToken)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);
        var reference = await mediator.Send(new GetTenantReferenceByIdQuery(tenantId), cancellationToken);
        return reference is null ? NotFound() : Ok(reference.ToResponse());
    }

    
    [HttpPatch("account-management/tenant")]
    [RequirePermission(Permissions.Edit)]
    public async Task<IActionResult> UpdateTenant([FromBody] UpdateTenantRequestDto request, CancellationToken cancellationToken)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);
        var userId = ClaimsResolver.GetUserId(User);

        await mediator.Send(new UpdateTenantCommand(tenantId, userId, request.Name), cancellationToken);
        return NoContent();
    }
}

