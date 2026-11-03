using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.ReactivateUserAccount;

internal sealed record ReactivateUserAccountCommand(
    Guid TenantId,
    Guid UserId,
    Guid UpdatedBy) : IRequest;
