using System.Security.Claims;
using MediatR;

namespace FieldService.Http.Cqrs.Commands.Login;

public record LoginCommand(
    Guid RequestId,
    ClaimsPrincipal User,
    string IpAddress,
    string UserAgent) : IRequest<Guid>;
  
