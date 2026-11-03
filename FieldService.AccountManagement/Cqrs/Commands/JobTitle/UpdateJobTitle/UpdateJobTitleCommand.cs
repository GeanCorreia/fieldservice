using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.UpdateJobTitle;

internal sealed record UpdateJobTitleCommand(
    Guid TenantId,
    Guid JobTitleId,
    Guid UpdatedBy,
    string? Title,
    string? Description) : IRequest;

