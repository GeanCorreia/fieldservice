using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.UserAccountTeamTransfer;

internal sealed record UserAccountTeamTransferCommand(
    Guid TenantId,
    Guid UserId,
    Guid TeamId,
    Guid UpdatedBy) : IRequest;
