using FieldService.Superset.Dtos;
using MediatR;

namespace FieldService.Superset.Cqrs.Commands.UpdateUser;

public record UpdateUserCommand(
    Guid UserId,
    Guid TenantId,
    SupersetUserUpdateRequest SupersetUserUpdateRequest
    ) : IRequest;