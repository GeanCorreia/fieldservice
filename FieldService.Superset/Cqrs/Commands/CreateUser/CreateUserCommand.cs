using FieldService.Shared.Types;
using MediatR;

namespace FieldService.Superset.Cqrs.Commands.CreateUser;

public record CreateUserCommand(
    IEnumerable<Permission> Permissions,
    Guid UserId,
    Guid TenantId,
    string FirstName,
    string LastName,
    string Email) : IRequest;