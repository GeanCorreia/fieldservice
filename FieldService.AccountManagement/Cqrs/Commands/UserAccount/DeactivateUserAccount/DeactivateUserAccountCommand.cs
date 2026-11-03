using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.DeactivateUserAccount;

internal sealed record DeactivateUserAccountCommand(
    Guid TenantId,
    Guid UserId,
    Guid UpdatedBy) : IRequest;
