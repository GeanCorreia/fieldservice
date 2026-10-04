using MediatR;

namespace FieldService.Superset.Cqrs.Commands.SuspendSupersetTenant;

public record SuspendSupersetTenantCommand(
    Guid TenantId) : IRequest;