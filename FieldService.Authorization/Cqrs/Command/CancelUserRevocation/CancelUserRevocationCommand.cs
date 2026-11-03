using MediatR;

namespace FieldService.Authorization.Cqrs.Command.CancelUserRevocation;

public sealed record CancelUserRevocationCommand(
    Guid UserId,
    Guid TenantId,
    Guid? CreatedBy = null,
    DateTimeOffset? CancelledAt = null) : IRequest;

