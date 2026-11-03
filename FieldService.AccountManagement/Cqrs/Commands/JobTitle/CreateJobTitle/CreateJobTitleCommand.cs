using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.CreateJobTitle;

internal sealed record CreateJobTitleCommand(
    Guid TenantId,
    Guid JobTitleId,
    string Title,
    string? Description,
    Guid CreatedBy) : IRequest;

