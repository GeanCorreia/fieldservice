using FieldService.Shared.Types;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.UpdateUser;

internal sealed record UpdateUserCommand(
    Guid TenantId,
    Guid UserId,
    Guid UpdatedBy,
    string? FullName,
    string? DisplayName,
    Email? Email,
    Phone? Phone,
    Guid? JobTitleId) : IRequest;

