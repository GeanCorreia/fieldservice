using FieldService.Http.Dtos;
using MediatR;

namespace FieldService.Http.Cqrs;

public record GetUserTenantsQuery() : IRequest<UserAuthenticationDto?>;