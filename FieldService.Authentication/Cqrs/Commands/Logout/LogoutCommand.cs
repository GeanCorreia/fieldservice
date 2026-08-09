using MediatR;

namespace FieldService.Authentication.Cqrs.Commands;

public record LogoutCommand() : IRequest;

