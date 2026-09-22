using MediatR;

namespace FieldService.Http.Cqrs.Commands;

public record LogoutCommand(Guid SessionId) : IRequest;

