using FieldService.Authorization.Events;
using MediatR;

namespace FieldService.Authorization.Cqrs.Command.SuspendUser;

public sealed record SuspendUserCommand(
    Guid UserId,
    Guid TenantId,
    SuspensionSource Source,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt = null,
    Guid? CreatedBy = null,
    DateTimeOffset? CreatedAt = null) : IRequest;

