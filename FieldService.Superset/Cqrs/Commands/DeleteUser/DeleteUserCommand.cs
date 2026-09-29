using MediatR;

namespace FieldService.Superset.Cqrs.Commands.DeleteUser;

public record DeleteUserCommand(
    Guid UserId,
    Guid TenantId) : IRequest;