using FieldService.AccountManagement.Cqrs.Commands.CreateTeam;
using FieldService.AccountManagement.Cqrs.Commands.UpdateTeam;
using FieldService.AccountManagement.Cqrs.Commands.DeactivateTeam;
using FieldService.AccountManagement.Cqrs.Commands.RemoveTeamLeader;
using FieldService.AccountManagement.Cqrs.Queries.GetTeamById;
using FieldService.AccountManagement.Cqrs.Queries.GetTeamsByTenantId;
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
public sealed class TeamController(IMediator mediator) : ControllerBase
{
    [HttpPost("account-management/teams/{teamId:guid}/deactivate")]
    [RequirePermission(Permissions.Edit)]
    public async Task<IActionResult> DeactivateTeam(
        [FromRoute] Guid teamId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DeactivateTeamRequestDto? request,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new DeactivateTeamCommand(
            ClaimsResolver.GetTenantId(User), teamId, ClaimsResolver.GetUserId(User), request?.TransferTeam),
            cancellationToken);
        return NoContent();
    }

    [HttpDelete("account-management/teams/{teamId:guid}/leader")]
    [RequirePermission(Permissions.Edit)]
    public async Task<IActionResult> RemoveLeader([FromRoute] Guid teamId, CancellationToken cancellationToken)
    {
        await mediator.Send(new RemoveTeamLeaderCommand(
            ClaimsResolver.GetTenantId(User), teamId, ClaimsResolver.GetUserId(User)), cancellationToken);
        return NoContent();
    }

    [HttpGet("account-management/teams")]
    [RequirePermission(Permissions.Read)]
    public async Task<IActionResult> GetTeams(CancellationToken cancellationToken)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);
        var teams = await mediator.Send(new GetTeamsByTenantIdQuery(tenantId), cancellationToken);
        return Ok(teams.Select(t => t.ToResponse()).ToList());
    }

    [HttpGet("account-management/teams/{teamId:guid}")]
    [RequirePermission(Permissions.Read)]
    public async Task<IActionResult> GetTeamById([FromRoute] Guid teamId, CancellationToken cancellationToken)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);
        var team = await mediator.Send(new GetTeamByIdQuery(tenantId, teamId), cancellationToken);
        return team is null ? NotFound() : Ok(team.ToResponse());
    }

    [HttpPost("account-management/teams")]
    [RequirePermission(Permissions.Edit)]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamRequestDto request, CancellationToken cancellationToken)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);
        var userId = ClaimsResolver.GetUserId(User);
        var teamId = request.TeamId is { } id && id != Guid.Empty ? id : Guid.NewGuid();

        await mediator.Send(
            new CreateTeamCommand(tenantId, teamId, request.Name, request.Description, request.LeaderId, userId),
            cancellationToken);

        return NoContent();
    }

    [HttpPatch("account-management/teams/{teamId:guid}")]
    [RequirePermission(Permissions.Edit)]
    public async Task<IActionResult> UpdateTeam(
        [FromRoute] Guid teamId,
        [FromBody] UpdateTeamRequestDto request,
        CancellationToken cancellationToken)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);
        var userId = ClaimsResolver.GetUserId(User);
        await mediator.Send(
            new UpdateTeamCommand(
                tenantId,
                teamId,
                userId,
                request.Name,
                request.Description,
                request.LeaderId),
            cancellationToken);
        return NoContent();
    }
}
