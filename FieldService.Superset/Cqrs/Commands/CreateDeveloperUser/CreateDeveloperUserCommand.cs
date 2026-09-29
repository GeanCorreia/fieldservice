using MediatR;

namespace FieldService.Superset.Cqrs.Commands.CreateDeveloperUser;

public record CreateDeveloperUserCommand(
    Guid UserId,
    Guid TenantId,
    string FirstName,
    string LastName,
    string Email,
    TimeSpan? AccessDuration = null) : IRequest;