using FieldService.Shared.Types;
using MediatR;

namespace FieldService.Http.Cqrs.Queries.GetUser;

public record GetUserQuery(
    Guid UserId) : IRequest<UserDto?>;