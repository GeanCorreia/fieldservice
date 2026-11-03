using FieldService.AccountManagement.Cqrs.Commands.CreateJobTitle;
using FieldService.AccountManagement.Cqrs.Commands.UpdateJobTitle;
using FieldService.AccountManagement.Cqrs.Commands.DeactivateJobTitle;
using FieldService.AccountManagement.Cqrs.Queries.GetJobTitleById;
using FieldService.AccountManagement.Cqrs.Queries.GetJobTitlesByTenantId;
using FieldService.AccountManagement.Dtos.Request;
using FieldService.AccountManagement.Dtos.Response;
using FieldService.Shared.Services;
using FieldService.Shared.Types;
using FieldService.Authorization.Attributes;
using Permissions = FieldService.AccountManagement.Attributes.Permissions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FieldService.AccountManagement.Controllers;

[ApiController]
public sealed class JobTitleController(IMediator mediator) : ControllerBase
{
    [HttpPost("account-management/job-titles/{jobTitleId:guid}/deactivate")]
    [RequirePermission(Permissions.Edit)]
    public async Task<IActionResult> DeactivateJobTitle(
        [FromRoute] Guid jobTitleId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DeactivateJobTitleRequestDto? request,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new DeactivateJobTitleCommand(
            ClaimsResolver.GetTenantId(User), jobTitleId, ClaimsResolver.GetUserId(User), request?.TransferJobTitle),
            cancellationToken);
        return NoContent();
    }

    [HttpGet("account-management/job-titles")]
    [RequirePermission(Permissions.Read)]
    public async Task<IActionResult> GetJobTitles(CancellationToken cancellationToken)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);
        var jobTitles = await mediator.Send(new GetJobTitlesByTenantIdQuery(tenantId), cancellationToken);
        return Ok(jobTitles.Select(j => j.ToResponse()).ToList());
    }

    [HttpGet("account-management/job-titles/{jobTitleId:guid}")]
    [RequirePermission(Permissions.Read)]
    public async Task<IActionResult> GetJobTitleById([FromRoute] Guid jobTitleId, CancellationToken cancellationToken)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);
        var jobTitle = await mediator.Send(new GetJobTitleByIdQuery(tenantId, jobTitleId), cancellationToken);
        return jobTitle is null ? NotFound() : Ok(jobTitle.ToResponse());
    }

    [HttpPost("account-management/job-titles")]
    [RequirePermission(Permissions.Edit)]
    public async Task<IActionResult> CreateJobTitle([FromBody] CreateJobTitleRequestDto request, CancellationToken cancellationToken)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);
        var userId = ClaimsResolver.GetUserId(User);
        var jobTitleId = request.JobTitleId is { } id && id != Guid.Empty ? id : Guid.NewGuid();

        await mediator.Send(
            new CreateJobTitleCommand(tenantId, jobTitleId, request.Title, request.Description, userId),
            cancellationToken);

        return NoContent();
    }

    [HttpPatch("account-management/job-titles/{jobTitleId:guid}")]
    [RequirePermission(Permissions.Edit)]
    public async Task<IActionResult> UpdateJobTitle(
        [FromRoute] Guid jobTitleId,
        [FromBody] UpdateJobTitleRequestDto request,
        CancellationToken cancellationToken)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);
        var userId = ClaimsResolver.GetUserId(User);
        await mediator.Send(
            new UpdateJobTitleCommand(
                tenantId,
                jobTitleId,
                userId,
                request.Title,
                request.Description),
            cancellationToken);
        return NoContent();
    }
}
