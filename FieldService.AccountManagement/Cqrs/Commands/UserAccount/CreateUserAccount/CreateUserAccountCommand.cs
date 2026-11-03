using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.CreateUserAccount;

internal sealed record CreateUserAccountCommand(
    Guid TenantId,
    Guid? TeamId,
    Guid JobTitleId,
    string FullName,
    string DisplayName,
    string Email,
    string Phone,
    Guid CreatedBy) : IRequest;

