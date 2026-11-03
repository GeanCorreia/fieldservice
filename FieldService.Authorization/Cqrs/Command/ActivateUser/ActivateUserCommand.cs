using FieldService.Authorization.Events;
using MediatR;

namespace FieldService.Authorization.Cqrs.Command.ActivateUser;

public sealed record ActivateUserCommand(
    Guid UserId,
    Guid TenantId,
    SuspensionSource Source,
    Guid? CreatedBy = null,
    DateTimeOffset? ActivatedAt = null) : IRequest;

