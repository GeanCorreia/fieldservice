using System.Security.Claims;
using MediatR;

namespace FieldService.Http.Cqrs.Commands.Login;

public record LoginCommand(
    Guid RequestId,
    Guid TenantId,
    ClaimsPrincipal User,
    string IpAddress,
    string UserAgent) : IRequest<Guid>;
  
