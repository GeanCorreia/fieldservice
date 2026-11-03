using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.CreateTenant;

internal sealed record CreateTenantCommand(string Name, Guid CreatedBy) : IRequest<Guid>;

