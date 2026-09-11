using MediatR;

namespace FieldService.Http.Cqrs.Commands.Login;

public record LoginCommand(Guid TenantId) : IRequest<Guid>;
  
