using MediatR;

namespace FieldService.Authentication.Cqrs.Commands.Login;

public record LoginCommand(Guid TenantId) : IRequest<Guid>;
  
