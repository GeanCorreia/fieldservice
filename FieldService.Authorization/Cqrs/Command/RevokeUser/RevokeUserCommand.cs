using MediatR;

namespace FieldService.Authorization.Cqrs.Command.RevokeUser;

public sealed record RevokeUserCommand(
    Guid UserId,
    Guid TenantId,
    Guid? CreatedBy = null,
    DateTimeOffset? RevokedAt = null) : IRequest;

