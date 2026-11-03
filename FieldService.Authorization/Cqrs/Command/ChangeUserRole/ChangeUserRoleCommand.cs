using FieldService.Shared.Types;
using MediatR;

namespace FieldService.Authorization.Cqrs.Command.ChangeUserRole;

public sealed record ChangeUserRoleCommand(
    Guid UserId,
    Guid TenantId,
    Role Role,
    Guid? CreatedBy = null,
    DateTimeOffset? ChangedAt = null) : IRequest;

