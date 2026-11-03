using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.UpdateTenant;

internal sealed record UpdateTenantCommand(
    Guid TenantId,
    Guid UpdatedBy,
    string? Name) : IRequest;

