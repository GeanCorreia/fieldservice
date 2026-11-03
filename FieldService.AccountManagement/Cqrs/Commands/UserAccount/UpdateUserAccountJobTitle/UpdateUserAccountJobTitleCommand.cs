using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.UpdateUserAccountJobTitle;

internal sealed record UpdateUserAccountJobTitleCommand(
    Guid TenantId,
    Guid UserId,
    Guid JobTitleId,
    Guid UpdatedBy) : IRequest;
