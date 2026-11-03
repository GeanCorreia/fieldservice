using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.DeactivateJobTitle;

internal sealed record DeactivateJobTitleCommand(
    Guid TenantId,
    Guid JobTitleId,
    Guid UpdatedBy,
    Guid? TransferJobTitle = null) : IRequest;
