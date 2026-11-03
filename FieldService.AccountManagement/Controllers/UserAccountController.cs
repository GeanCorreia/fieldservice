using FieldService.AccountManagement.Cqrs.Commands.CreateUserAccount;
using FieldService.AccountManagement.Cqrs.Commands.UpdateUser;
using FieldService.AccountManagement.Cqrs.Commands.DeactivateUserAccount;
using FieldService.AccountManagement.Cqrs.Commands.ReactivateUserAccount;
using FieldService.AccountManagement.Cqrs.Commands.UserAccountTeamTransfer;
using FieldService.AccountManagement.Cqrs.Commands.UpdateUserAccountJobTitle;
using FieldService.AccountManagement.Cqrs.Queries.GetUserById;
using FieldService.AccountManagement.Cqrs.Queries.GetUsersByTeamId;
using FieldService.AccountManagement.Cqrs.Queries.GetUsersByTenantId;
using FieldService.AccountManagement.Dtos.Request;
using FieldService.AccountManagement.Dtos.Response;
using FieldService.Shared.Services;
using FieldService.Shared.Types;
using FieldService.Shared.Types.Http;
using FieldService.Authorization.Attributes;
using Permissions = FieldService.AccountManagement.Attributes.Permissions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FieldService.AccountManagement.Controllers;

[ApiController]
public sealed class UserAccountController(IMediator mediator) : ControllerBase
{
    [HttpGet("account-management/users")]
    [RequirePermission(Permissions.Read)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);
        var users = await mediator.Send(new GetUsersByTenantIdQuery(tenantId), cancellationToken);
        return Ok(users.Select(u => u.ToResponse()).ToList());
    }

    [HttpGet("account-management/users/{userId:guid}")]
    [RequirePermission(Permissions.Read)]
    public async Task<IActionResult> GetUserById([FromRoute] Guid userId, CancellationToken cancellationToken)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);
        var user = await mediator.Send(new GetUserByIdQuery(tenantId, userId), cancellationToken);
        return user is null ? NotFound() : Ok(user.ToResponse());
    }

    [HttpGet("account-management/users/paged")]
    [RequirePermission(Permissions.Read)]
    public async Task<IActionResult> GetUsersPage([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetUsersPageByTenantIdQuery(ClaimsResolver.GetTenantId(User), request), cancellationToken);
        return Ok(new PaginatedResult<UserAccountResponseDto>(
            result.Pagination, result.Data.Select(user => user.ToResponse()).ToList()));
    }

    [HttpGet("account-management/teams/{teamId:guid}/users/paged")]
    [RequirePermission(Permissions.Read)]
    public async Task<IActionResult> GetUsersPageByTeam(
        [FromRoute] Guid teamId, [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetUsersPageByTeamIdQuery(ClaimsResolver.GetTenantId(User), teamId, request), cancellationToken);
        return Ok(new PaginatedResult<UserAccountResponseDto>(
            result.Pagination, result.Data.Select(user => user.ToResponse()).ToList()));
    }

    [HttpPost("account-management/users/{userId:guid}/deactivate")]
    [RequirePermission(Permissions.Edit)]
    public async Task<IActionResult> DeactivateUser([FromRoute] Guid userId, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeactivateUserAccountCommand(
            ClaimsResolver.GetTenantId(User), userId, ClaimsResolver.GetUserId(User)), cancellationToken);
        return NoContent();
    }

    [HttpPost("account-management/users/{userId:guid}/reactivate")]
    [RequirePermission(Permissions.Edit)]
    public async Task<IActionResult> ReactivateUser([FromRoute] Guid userId, CancellationToken cancellationToken)
    {
        await mediator.Send(new ReactivateUserAccountCommand(
            ClaimsResolver.GetTenantId(User), userId, ClaimsResolver.GetUserId(User)), cancellationToken);
        return NoContent();
    }

    [HttpPatch("account-management/users/{userId:guid}/team")]
    [RequirePermission(Permissions.Edit)]
    public async Task<IActionResult> TransferTeam(
        [FromRoute] Guid userId, [FromBody] TransferUserTeamRequestDto request, CancellationToken cancellationToken)
    {
        await mediator.Send(new UserAccountTeamTransferCommand(
            ClaimsResolver.GetTenantId(User), userId, request.TeamId, ClaimsResolver.GetUserId(User)), cancellationToken);
        return NoContent();
    }

    [HttpPatch("account-management/users/{userId:guid}/job-title")]
    [RequirePermission(Permissions.Edit)]
    public async Task<IActionResult> ChangeJobTitle(
        [FromRoute] Guid userId, [FromBody] ChangeUserJobTitleRequestDto request, CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateUserAccountJobTitleCommand(
            ClaimsResolver.GetTenantId(User), userId, request.JobTitleId, ClaimsResolver.GetUserId(User)), cancellationToken);
        return NoContent();
    }

    [HttpGet("account-management/teams/{teamId:guid}/users")]
    [RequirePermission(Permissions.Read)]
    public async Task<IActionResult> GetUsersByTeam([FromRoute] Guid teamId, CancellationToken cancellationToken)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);
        var users = await mediator.Send(new GetUsersByTeamIdQuery(tenantId, teamId), cancellationToken);
        return Ok(users.Select(u => u.ToResponse()).ToList());
    }

    [HttpPost("account-management/users")]
    [RequirePermission(Permissions.Edit)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserAccountRequestDto request, CancellationToken cancellationToken)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);
        var createdBy = ClaimsResolver.GetUserId(User);
        await mediator.Send(
            new CreateUserAccountCommand(
                tenantId,
                request.TeamId,
                request.JobTitleId,
                request.FullName,
                request.DisplayName,
                request.Email,
                request.Phone,
                createdBy),
            cancellationToken);

        return NoContent();
    }

    [HttpPatch("account-management/users/{userId:guid}")]
    [RequirePermission(Permissions.Edit)]
    public async Task<IActionResult> UpdateUser(
        [FromRoute] Guid userId,
        [FromBody] UpdateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);
        var updatedBy = ClaimsResolver.GetUserId(User);
        await mediator.Send(
            new UpdateUserCommand(
                tenantId,
                userId,
                updatedBy,
                request.FullName,
                request.DisplayName,
                string.IsNullOrWhiteSpace(request.Email) ? null : new Email(request.Email),
                string.IsNullOrWhiteSpace(request.Phone) ? null : new Phone(request.Phone),
                request.JobTitleId),
            cancellationToken);

        return NoContent();
    }

    [HttpPatch("account-management/users/personalinfo")]
    [RequirePermission(Permissions.Edit)]
    public async Task<IActionResult> UpdateMyPersonalInfo(
        [FromBody] UpdateUserPersonalInfoRequestDto request,
        CancellationToken cancellationToken)
    {
        var tenantId = ClaimsResolver.GetTenantId(User);
        var userId = ClaimsResolver.GetUserId(User);

        await mediator.Send(
            new UpdateUserCommand(
                tenantId,
                userId,
                userId,
                request.FullName,
                request.DisplayName,
                string.IsNullOrWhiteSpace(request.Email) ? null : new Email(request.Email),
                string.IsNullOrWhiteSpace(request.Phone) ? null : new Phone(request.Phone),
                null),
            cancellationToken);

        return NoContent();
    }
}
